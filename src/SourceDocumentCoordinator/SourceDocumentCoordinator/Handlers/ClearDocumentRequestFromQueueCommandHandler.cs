using System;
using System.IO;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Helpers;
using Common.Messages.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus;
using Serilog.Context;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.Enums;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.Handlers
{
    public class ClearDocumentRequestFromQueueCommandHandler : IHandleMessages<ClearDocumentRequestFromQueueCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IDocumentStatusFactory _documentStatusFactory;
        private readonly ILogger<ClearDocumentRequestFromQueueCommandHandler> _logger;

        public ClearDocumentRequestFromQueueCommandHandler(IDataServiceApiClient dataServiceApiClient,
                                                            WorkflowDbContext dbContext, 
                                                            IOptionsSnapshot<GeneralConfig> generalConfig, 
                                                            IDocumentStatusFactory documentStatusFactory, 
                                                            ILogger<ClearDocumentRequestFromQueueCommandHandler> logger)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
            _generalConfig = generalConfig;
            _documentStatusFactory = documentStatusFactory;
            _logger = logger;
        }

        public async Task Handle(ClearDocumentRequestFromQueueCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(ClearDocumentRequestFromQueueCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var returnCode = await _dataServiceApiClient
                .DeleteDocumentRequestJobFromQueue(
                                                    _generalConfig.Value.CallerCode,
                                                    message.SourceDocumentId,
                                                    _generalConfig.Value.SourceDocumentWriteableFolderName)
                .ConfigureAwait(false);

            LogContext.PushProperty("ReturnCode", returnCode.ToJSONSerializedString());

            if (string.IsNullOrWhiteSpace(returnCode.Message) || !Path.IsPathRooted(returnCode.Message.Trim()))
            {
                _logger.LogError("A call to DeleteDocumentRequestJobFromQueue did not return a path to the generated source document; ReturnCode: {ReturnCode}");

                throw new ApplicationException($"A call to DeleteDocumentRequestJobFromQueue did not return a path to the generated source document; ReturnCode: {returnCode.ToJSONSerializedString()}");
            }

            if (returnCode.Code.HasValue)
            {
                switch (returnCode.Code.Value)
                {
                    case (int)ClearFromQueueReturnCodeEnum.Success:
                    case (int)ClearFromQueueReturnCodeEnum.Warning:
                        await SourceDocumentHelper.UpdateSourceDocumentStatus(
                                                    _documentStatusFactory, 
                                                    message.ProcessId, 
                                                    message.SourceDocumentId, 
                                                    SourceDocumentRetrievalStatus.FileGenerated, 
                                                    message.SourceType, message.CorrelationId, generatedFullFilename:returnCode.Message.Trim());
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
