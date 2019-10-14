using Common.Messages.Commands;

using NServiceBus;

using SourceDocumentCoordinator.HttpClients;

using System;
using System.Linq;
using System.Threading.Tasks;

using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetForwardDocumentLinksCommandHandler : IHandleMessages<GetForwardDocumentLinksCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;

        public GetForwardDocumentLinksCommandHandler(WorkflowDbContext dbContext, IDataServiceApiClient dataServiceApiClient)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
        }
        public async Task Handle(GetForwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            var docLinks = await _dataServiceApiClient.GetForwardDocumentLinks(message.SourceDocumentId);
            var linkedDocIds = docLinks.Select(s => s.DocId2);

            var documentObjects = await _dataServiceApiClient.GetDocumentsFromList(linkedDocIds.ToArray());

            foreach (var documentObject in documentObjects)
            {
                var linkedDocument = new LinkedDocument
                {
                    SdocId = message.SourceDocumentId,
                    LinkedSdocId = documentObject.Id,
                    RsdraNumber = documentObject.SourceName,
                    SourceDocumentName = documentObject.Name,
                    LinkType = "Forward",
                    Created = DateTime.Now
                };

                _dbContext.Add(linkedDocument);
                _dbContext.SaveChanges();
            }
        }
    }
}
