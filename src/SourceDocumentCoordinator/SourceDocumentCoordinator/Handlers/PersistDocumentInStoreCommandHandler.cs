using System.IO;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus;
using Serilog.Context;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.Handlers
{
    public class PersistDocumentInStoreCommandHandler : IHandleMessages<PersistDocumentInStoreCommand>
    {
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IContentServiceApiClient _contentServiceApiClient;
        private readonly WorkflowDbContext _dbContext;
        private readonly IDocumentStatusFactory _documentStatusFactory;
        private readonly IDocumentFileLocationFactory _documentFileLocationFactory;
        private readonly ILogger<PersistDocumentInStoreCommandHandler> _logger;

        public PersistDocumentInStoreCommandHandler(IOptionsSnapshot<GeneralConfig> generalConfig, 
                                                    IContentServiceApiClient contentServiceApiClient, 
                                                    WorkflowDbContext dbContext,
                                                    IDocumentStatusFactory documentStatusFactory,
                                                    IDocumentFileLocationFactory documentFileLocationFactory,
                                                    ILogger<PersistDocumentInStoreCommandHandler> logger)
        {
            _generalConfig = generalConfig;
            _contentServiceApiClient = contentServiceApiClient;
            _dbContext = dbContext;
            _documentStatusFactory = documentStatusFactory;
            _documentFileLocationFactory = documentFileLocationFactory;
            _logger = logger;
        }

        /// <summary>
        /// Read file from SDRA output folder, post to Content Service and store returned Guid in db.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(PersistDocumentInStoreCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(PersistDocumentInStoreCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");


            var fileBytes = File.ReadAllBytes(message.Filepath);

            var newGuid = await _contentServiceApiClient.Post(fileBytes, Path.GetFileName(message.Filepath));

            await SourceDocumentHelper.UpdateSourceDocumentStatus(
                                                                    _documentStatusFactory,
                                                                    message.ProcessId,
                                                                    message.SourceDocumentId,
                                                                    SourceDocumentRetrievalStatus.FileGenerated,
                                                                    message.SourceType, message.CorrelationId);
            
            await SourceDocumentHelper.UpdateSourceDocumentFileLocation(
                                                                        _documentFileLocationFactory,
                                                                        message.ProcessId,
                                                                        message.SourceDocumentId,
                                                                        message.SourceType, newGuid, message.Filepath);
        }
    }
}
