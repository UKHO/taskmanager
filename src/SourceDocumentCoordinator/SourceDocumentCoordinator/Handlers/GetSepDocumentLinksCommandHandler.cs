using Common.Messages.Commands;

using NServiceBus;

using SourceDocumentCoordinator.HttpClients;

using System;
using System.Threading.Tasks;

using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetSepDocumentLinksCommandHandler : IHandleMessages<GetSepDocumentLinksCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;

        public GetSepDocumentLinksCommandHandler(WorkflowDbContext dbContext, IDataServiceApiClient dataServiceApiClient)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
        }
        public async Task Handle(GetSepDocumentLinksCommand message, IMessageHandlerContext context)
        {
            var docLinks = await _dataServiceApiClient.GetSepDocumentLinks(message.SourceDocumentId);
            foreach (var documentObject in docLinks)
            {
                var linkedDocument = new LinkedDocument
                {
                    SdocId = message.SourceDocumentId,
                    LinkedSdocId = documentObject.Id,
                    RsdraNumber = documentObject.SourceName,
                    SourceDocumentName = documentObject.Name,
                    LinkType = "SEP",
                    Created = DateTime.Now
                };

                _dbContext.Add(linkedDocument);
                _dbContext.SaveChanges();
            }

        }
    }
}
