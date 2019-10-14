using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetBackwardDocumentLinksCommandHandler : IHandleMessages<GetBackwardDocumentLinksCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;

        public GetBackwardDocumentLinksCommandHandler(IDataServiceApiClient dataServiceApiClient, WorkflowDbContext dbContext)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
        }

        public async Task Handle(GetBackwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            var docLinks = await _dataServiceApiClient.GetBackwardDocumentLinks(message.SourceDocumentId);
            var linkedDocIds = docLinks.Select(s => s.DocId2).ToList();

            if (linkedDocIds?.Count == 0) return;

            var documentObjects = await _dataServiceApiClient.GetDocumentsFromList(linkedDocIds.ToArray());

            foreach (var documentObject in documentObjects)
            {
                var linkedDocument = new LinkedDocument
                {
                    SdocId = message.SourceDocumentId,
                    LinkedSdocId = documentObject.Id,
                    RsdraNumber = documentObject.SourceName,
                    SourceDocumentName = documentObject.Name,
                    LinkType = "Backward",
                    Created = DateTime.Now
                };

                _dbContext.Add(linkedDocument);
                _dbContext.SaveChanges();
            }

        }
    }
}
