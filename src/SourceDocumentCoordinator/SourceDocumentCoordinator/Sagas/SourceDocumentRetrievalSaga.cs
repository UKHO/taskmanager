using Common.Helpers;
using Microsoft.Extensions.Options;

using NServiceBus;
using NServiceBus.Logging;

using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;

using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Messages.Enums;
using Common.Messages.Events;
using DataServices.Models;
using SourceDocumentCoordinator.Enums;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.Sagas
{
    public class SourceDocumentRetrievalSaga : Saga<SourceDocumentRetrievalSagaData>,
        IAmStartedByMessages<InitiateSourceDocumentRetrievalEvent>,
        IHandleTimeouts<GetDocumentRequestQueueStatusCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IDocumentStatusFactory _documentStatusFactory;
        ILog log = LogManager.GetLogger<SourceDocumentRetrievalSaga>();

        public SourceDocumentRetrievalSaga(WorkflowDbContext dbContext, IDataServiceApiClient dataServiceApiClient,
            IOptionsSnapshot<GeneralConfig> generalConfig, IDocumentStatusFactory documentStatusFactory)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _generalConfig = generalConfig;
            _documentStatusFactory = documentStatusFactory;
        }

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SourceDocumentRetrievalSagaData> mapper)
        {
            mapper.ConfigureMapping<InitiateSourceDocumentRetrievalEvent>(message => message.SourceDocumentId)
                .ToSaga(sagaData => sagaData.SourceDocumentId);
        }

        public async Task Handle(InitiateSourceDocumentRetrievalEvent message, IMessageHandlerContext context)
        {
            log.Debug($"Handling {nameof(InitiateSourceDocumentRetrievalEvent)}: {message.ToJSONSerializedString()}");

            if (!Data.IsStarted)
            {
                Data.IsStarted = true;
                Data.CorrelationId = message.CorrelationId;
                Data.ProcessId = message.ProcessId;
                Data.SourceDocumentId = message.SourceDocumentId;
                Data.DocumentStatusId = 0;
                Data.DocumentType = message.DocumentType;
            }

            // Call GetDocumentForViewing method on DataServices API
            var returnCode = await _dataServiceApiClient.GetDocumentForViewing(_generalConfig.Value.CallerCode,
                message.SourceDocumentId,
                _generalConfig.Value.SourceDocumentWriteableFolderName,
                message.GeoReferenced);

            await ProcessGetDocumentForViewingErrorCodes(returnCode, message);

            if (Data.DocumentStatusId > 0)
            {
                var requestStatus = new GetDocumentRequestQueueStatusCommand
                {
                    SourceDocumentId = message.SourceDocumentId,
                    CorrelationId = message.CorrelationId,
                    DocumentType = message.DocumentType
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
            var sourceDocument = queuedDocs.Result.First(x => x.SodcId == message.SourceDocumentId);

            if (sourceDocument.Code == null)
                throw new ApplicationException(
                    $"Source Document Retrieval Status Code is null {Environment.NewLine}{sourceDocument.ToJSONSerializedString()}");

            await ProcessDocumentRequestQueueErrorStatus(message, context, sourceDocument);
        }

        private async Task ProcessGetDocumentForViewingErrorCodes(ReturnCode returnCode, InitiateSourceDocumentRetrievalEvent message)
        {
            switch ((QueueForRetrievalReturnCodeEnum)returnCode.Code.Value)
            {
                case QueueForRetrievalReturnCodeEnum.Success:
                    Data.DocumentStatusId = await SourceDocumentHelper.UpdateSourceDocumentStatus(_documentStatusFactory, message.ProcessId, message.SourceDocumentId, SourceDocumentRetrievalStatus.Started, message.DocumentType);
                    break;
                case QueueForRetrievalReturnCodeEnum.AlreadyQueued:
                    if (Data.DocumentStatusId < 1)
                    {
                        Data.DocumentStatusId = await SourceDocumentHelper.UpdateSourceDocumentStatus(_documentStatusFactory, message.ProcessId, message.SourceDocumentId, SourceDocumentRetrievalStatus.Started, message.DocumentType);
                    }
                    break;
                case QueueForRetrievalReturnCodeEnum.QueueInsertionFailed:
                    MarkAsComplete();
                    throw new ApplicationException($"Unable to queue source document for retrieval: {Environment.NewLine}{returnCode.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                case QueueForRetrievalReturnCodeEnum.SdocIdNotRecognised:
                    MarkAsComplete();
                    throw new ApplicationException($"Source document Id not recognised when queuing document for retrieval: {Environment.NewLine}{returnCode.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                default:
                    MarkAsComplete();
                    throw new NotImplementedException($"Return code from GetDocumentForViewing not implemented: {Environment.NewLine}{returnCode.Message}{Environment.NewLine}" +
                                                      $"{returnCode}");
            }
        }

        private async Task ProcessDocumentRequestQueueErrorStatus(GetDocumentRequestQueueStatusCommand message,
            IMessageHandlerContext context, QueuedDocumentObject sourceDocument)
        {
            switch ((RequestQueueStatusReturnCodeEnum)sourceDocument.Code.Value)
            {
                case RequestQueueStatusReturnCodeEnum.Success:
                    // Doc Ready; update DB;
                    await SourceDocumentHelper.UpdateSourceDocumentStatus(_documentStatusFactory, Data.ProcessId, Data.SourceDocumentId, SourceDocumentRetrievalStatus.Ready, Data.DocumentType);

                    var removeFromQueue = new ClearDocumentRequestFromQueueCommand
                    {
                        CorrelationId = message.CorrelationId,
                        ProcessId = Data.ProcessId,
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
                case RequestQueueStatusReturnCodeEnum.Queued:
                    // Still queued; fire another timer
                    await RequestTimeout<GetDocumentRequestQueueStatusCommand>(context,
                        TimeSpan.FromSeconds(_generalConfig.Value
                            .SourceDocumentCoordinatorQueueStatusIntervalSeconds),
                        message);
                    break;
                case RequestQueueStatusReturnCodeEnum.ConversionFailed:
                case RequestQueueStatusReturnCodeEnum.ConversionTimeOut:
                case RequestQueueStatusReturnCodeEnum.NotSuitableForConversion:
                case RequestQueueStatusReturnCodeEnum.NotGeoreferenced:

                    MarkAsComplete();

                    var msg = new InitiateSourceDocumentRetrievalEvent
                    {
                        CorrelationId = message.CorrelationId,
                        ProcessId = Data.ProcessId,
                        SourceDocumentId = message.SourceDocumentId,
                        GeoReferenced = false
                    };
                    await context.SendLocal(msg);
                    break;
                case RequestQueueStatusReturnCodeEnum.FolderNotWritable:
                    MarkAsComplete();
                    throw new ApplicationException($"Source document folder not writeable: {Environment.NewLine}{sourceDocument.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                case RequestQueueStatusReturnCodeEnum.NoDocumentFound:
                    MarkAsComplete();
                    throw new ApplicationException($"Cannot find source document: {Environment.NewLine}{sourceDocument.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                case RequestQueueStatusReturnCodeEnum.QueueInsertionFailed:
                    MarkAsComplete();
                    throw new ApplicationException($"Unable to queue source document for retrieval: {Environment.NewLine}{sourceDocument.Message}{Environment.NewLine}" +
                                                   $"{message.ToJSONSerializedString()}");
                default:
                    MarkAsComplete();
                    throw new NotImplementedException($"sourceDocument.Code: {Environment.NewLine}{sourceDocument.Message}{Environment.NewLine}" +
                                                      $"{sourceDocument.Code}");
            }
        }

        //private void UpdateSourceDocumentStatus(GetDocumentRequestQueueStatusCommand message)
        //{
        //    var primaryDocumentStatus =
        //        _dbContext.PrimaryDocumentStatus.FirstOrDefault(s => s.SdocId == message.SourceDocumentId);
        //    if (primaryDocumentStatus != null)
        //    {
        //        primaryDocumentStatus.Status = SourceDocumentRetrievalStatus.Ready.ToString();
        //        _dbContext.SaveChanges();
        //    }
        //}
    }
}
