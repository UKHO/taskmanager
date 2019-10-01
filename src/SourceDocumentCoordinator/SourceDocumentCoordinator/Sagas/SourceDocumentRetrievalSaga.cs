using Common.Helpers;
using Common.Messages.Commands;

using Microsoft.Extensions.Options;

using NServiceBus;
using NServiceBus.Logging;

using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;

using System;
using System.Linq;
using System.Threading.Tasks;
using DataServices.Models;
using SourceDocumentCoordinator.Enums;
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
            var returnCode = await _dataServiceApiClient.GetDocumentForViewing(_generalConfig.Value.CallerCode,
                message.SourceDocumentId,
                _generalConfig.Value.SourceDocumentWriteableFolderName,
                true);

            var result = HandleSourceDocumentErrorCodes(returnCode.Code.Value, message);

            if (!result)
            {
                // TODO: throw exception
                return;
            }

            if (Data.SourceDocumentStatusId > 0)
            {
                var requestStatus = new GetDocumentRequestQueueStatusCommand
                {
                    SourceDocumentId = message.SourceDocumentId,
                    CorrelationId = message.CorrelationId
                };

                await RequestTimeout<GetDocumentRequestQueueStatusCommand>(context,
                    TimeSpan.FromSeconds(_generalConfig.Value.SourceDocumentCoordinatorQueueStatusIntervalSeconds),
                    requestStatus);
            }
        }

        private int AddSourceDocumentStatus(InitiateSourceDocumentRetrievalCommand message)
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

            return sourceDocumentStatus.SourceDocumentStatusId;
        }

        public async Task Timeout(GetDocumentRequestQueueStatusCommand message, IMessageHandlerContext context)
        {
            var queuedDocs = _dataServiceApiClient.GetDocumentRequestQueueStatus(_generalConfig.Value.CallerCode);

            // TODO: Potentially deal with a list of queued requests...
            var sourceDocument = queuedDocs.Result.First(x => x.SodcId == message.SourceDocumentId);

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
                        SourceDocumentId = message.SourceDocumentId
                    };
                    await context.SendLocal(removeFromQueue).ConfigureAwait(false);

                    // Fire command to store source doc in Content Service
                    var persistCommand = new PersistDocumentInStoreCommand
                    {
                        CorrelationId = message.CorrelationId,
                        SourceDocumentId = message.SourceDocumentId,
                        Filepath = sourceDocument.Message
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
                    break; ;
                default:
                    throw new NotImplementedException($"sourceDocument.Code: {sourceDocument.Code}");
            }
        }

        private bool HandleSourceDocumentErrorCodes(int returnCode, InitiateSourceDocumentRetrievalCommand message)
        {
            switch ((QueueForRetrievalReturnCodeEnum)returnCode)
            {
                case QueueForRetrievalReturnCodeEnum.Success:
                    Data.SourceDocumentStatusId = AddSourceDocumentStatus(message);
                    return true;
                case QueueForRetrievalReturnCodeEnum.AlreadyQueued:
                    if (Data.SourceDocumentStatusId < 1)
                    {
                        Data.SourceDocumentStatusId = AddSourceDocumentStatus(message);
                    }
                    return true;
                case QueueForRetrievalReturnCodeEnum.QueueInsertionFailed:
                    return false;
                case QueueForRetrievalReturnCodeEnum.SdocIdNotRecognised:
                    return false;
                default:
                    throw new NotImplementedException($"Return code from GetDocumentForViewing not implemented: {returnCode}");
            }
        }

        private void UpdateSourceDocumentStatus(GetDocumentRequestQueueStatusCommand message)
        {
            var sourceDocumentStatus =
                _dbContext.SourceDocumentStatus.FirstOrDefault(s => s.SdocId == message.SourceDocumentId);
            if (sourceDocumentStatus != null)
            {
                sourceDocumentStatus.Status = SourceDocumentRetrievalStatus.Ready.ToString();
                _dbContext.SaveChanges();
            }
        }
    }
}
