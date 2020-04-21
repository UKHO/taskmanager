using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus;
using Serilog.Context;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;

namespace SourceDocumentCoordinator.Handlers
{
    public class ClearDocumentRequestFromQueueCommandHandler : IHandleMessages<ClearDocumentRequestFromQueueCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly ILogger<ClearDocumentRequestFromQueueCommandHandler> _logger;

        public ClearDocumentRequestFromQueueCommandHandler(IDataServiceApiClient dataServiceApiClient,
                                                            IOptionsSnapshot<GeneralConfig> generalConfig,
                                                            ILogger<ClearDocumentRequestFromQueueCommandHandler> logger)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _generalConfig = generalConfig;
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

            _logger.LogInformation("A call to DeleteDocumentRequestJobFromQueue completed with ReturnCode: {ReturnCode}");

        }
    }
}
