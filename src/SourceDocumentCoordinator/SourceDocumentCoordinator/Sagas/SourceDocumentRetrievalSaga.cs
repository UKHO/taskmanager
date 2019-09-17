using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Logging;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace SourceDocumentCoordinator.Sagas
{
    public class SourceDocumentRetrievalSaga : Saga<SourceDocumentRetrievalSagaData>,
        IAmStartedByMessages<InitiateSourceDocumentRetrievalCommand>,
        IHandleTimeouts<GetDocumentRequestQueueStatusCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        ILog log = LogManager.GetLogger<SourceDocumentRetrievalSaga>();

        public SourceDocumentRetrievalSaga(WorkflowDbContext dbContext, IDataServiceApiClient dataServiceApiClient,
            IOptionsSnapshot<GeneralConfig> generalConfig)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _generalConfig = generalConfig;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SourceDocumentRetrievalSagaData> mapper)
        {
            mapper.ConfigureMapping<InitiateSourceDocumentRetrievalCommand>(message => message.SourceDocumentId)
                .ToSaga(sagaData => sagaData.SourceDocumentId);
        }

        public async Task Handle(InitiateSourceDocumentRetrievalCommand message, IMessageHandlerContext context)
        {
            log.Debug($"Handling {nameof(InitiateSourceDocumentRetrievalCommand)}: {message.ToJSONSerializedString()}");

            if (!Data.IsStarted)
            {
                Data.IsStarted = true;
                Data.CorrelationId = message.CorrelationId;
                Data.SourceDocumentId = message.SourceDocumentId;
                Data.SourceDocumentStatusId = 0;
            }

            // Call GetDocumentForViewing method on DataServices API
            var returnCode = await _dataServiceApiClient.GetDocumentForViewing(_generalConfig.Value.CallerCode, message.SourceDocumentId, "tbc",
                true);

            // TODO: Think about different return code scenarios
            if (returnCode.Code.HasValue
                && (returnCode.Code.Value == 0)
                || (returnCode.Code.Value == 1 && Data.SourceDocumentStatusId == 0))
            {
                var sourceDocumentStatus = new SourceDocumentStatus
                {
                    ProcessId = message.ProcessId,
                    SdocId = message.SourceDocumentId,
                    Status = SourceDocumentRetrievalStatus.Started.ToString(),
                    StartedAt = DateTime.Now
                };

                _dbContext.SourceDocumentStatus.Add(sourceDocumentStatus);

                _dbContext.SaveChanges();

                Data.SourceDocumentStatusId = sourceDocumentStatus.SourceDocumentStatusId;
            }

            if (Data.SourceDocumentStatusId > 0)
            {
                var requestStatus = new GetDocumentRequestQueueStatusCommand
                {
                    SdocId = message.SourceDocumentId,
                    CorrelationId = message.CorrelationId
                };

                await RequestTimeout<GetDocumentRequestQueueStatusCommand>(context,
                    TimeSpan.FromSeconds(_generalConfig.Value.SourceDocumentCoordinatorQueueStatusIntervalSeconds),
                    requestStatus);
            }
        }

        public async Task Timeout(GetDocumentRequestQueueStatusCommand message, IMessageHandlerContext context)
        {
            var queuedDocs = _dataServiceApiClient.GetDocumentRequestQueueStatus(_generalConfig.Value.CallerCode);

            // TODO: Potentially deal with a list of queued requests...
            var sourceDocument = queuedDocs.Result.First(x => x.SodcId == message.SdocId);

            if (sourceDocument.Code == null)
                throw new ApplicationException(
                    $"Source Document Retrieval Status Code is null {Environment.NewLine}{sourceDocument.ToJSONSerializedString()}");

            switch (sourceDocument.Code.Value)
            {
                case 0:
                    // Doc Ready; update DB;
                    UpdateSourceDocumentStatus(message);

                    // TODO: Subsequent stories:
                    // TODO: Once document has been fetched, call ClearDocumentRequestJobFromQueue on DataServices API and close saga...
                    var removeFromQueue = new ClearDocumentRequestFromQueueCommand
                    {
                        CorrelationId = message.CorrelationId,
                        SdocId = message.SdocId
                    };
                    await context.SendLocal(removeFromQueue).ConfigureAwait(false);

                    // TODO: fire new Command to retrieve the doc and save it in Content service
                    var persistCommand = new PersistDocumentInStoreCommand
                    {
                        CorrelationId = message.CorrelationId,
                        SdocId = message.SdocId
                    };
                    await context.SendLocal(persistCommand).ConfigureAwait(false);

                    MarkAsComplete();

                    break;
                case 1:
                    // Still queued; fire another timer
                    await RequestTimeout<GetDocumentRequestQueueStatusCommand>(context,
                        TimeSpan.FromSeconds(_generalConfig.Value
                            .SourceDocumentCoordinatorQueueStatusIntervalSeconds),
                        message);
                    break;;
                default:
                    throw new NotImplementedException($"sourceDocument.Code: {sourceDocument.Code}");
            }
        }

        private void UpdateSourceDocumentStatus(GetDocumentRequestQueueStatusCommand message)
        {
            var sourceDocumentStatus =
                _dbContext.SourceDocumentStatus.FirstOrDefault(s => s.SdocId == message.SdocId);
            if (sourceDocumentStatus != null)
            {
                sourceDocumentStatus.Status = SourceDocumentRetrievalStatus.Ready.ToString();
                _dbContext.SaveChanges();
            }
        }
    }
}
