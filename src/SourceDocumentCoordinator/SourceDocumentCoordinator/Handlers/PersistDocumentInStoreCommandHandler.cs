using System.Threading.Tasks;
using NServiceBus;
using SourceDocumentCoordinator.Messages;
using System.IO;
using Common.Factories;
using Common.Factories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.Handlers
{
    public class PersistDocumentInStoreCommandHandler : IHandleMessages<PersistDocumentInStoreCommand>
    {
        private readonly IOptionsSnapshot<GeneralConfig> _generalConfig;
        private readonly IContentServiceApiClient _contentServiceApiClient;
        private readonly WorkflowDbContext _dbContext;
        private readonly IDocumentFileLocationFactory _documentFileLocationFactory;

        public PersistDocumentInStoreCommandHandler(IOptionsSnapshot<GeneralConfig> generalConfig, 
                                                    IContentServiceApiClient contentServiceApiClient, 
                                                    WorkflowDbContext dbContext, 
                                                    IDocumentFileLocationFactory documentFileLocationFactory)
        {
            _generalConfig = generalConfig;
            _contentServiceApiClient = contentServiceApiClient;
            _dbContext = dbContext;
            _documentFileLocationFactory = documentFileLocationFactory;
        }

        /// <summary>
        /// Read file from SDRA output folder, post to Content Service and store returned Guid in db.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Handle(PersistDocumentInStoreCommand message, IMessageHandlerContext context)
        {
            var fileBytes = File.ReadAllBytes(message.Filepath);

            var newGuid = await _contentServiceApiClient.Post(fileBytes, Path.GetFileName(message.Filepath));

            await SourceDocumentHelper.UpdateSourceDocumentFileLocation(
                                                                        _documentFileLocationFactory,
                                                                        message.ProcessId,
                                                                        message.SourceDocumentId,
                                                                        message.SourceType, newGuid, message.Filepath);
        }
    }
}
