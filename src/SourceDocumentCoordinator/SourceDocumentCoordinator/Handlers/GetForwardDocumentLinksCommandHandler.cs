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
            var linkedDocIds = docLinks.Select(s => s.DocId2).ToList();

            if (linkedDocIds?.Count == 0) return;

            foreach (var linkedDocId in linkedDocIds)
            {
                var documentAssessmentData = await _dataServiceApiClient.GetAssessmentData(linkedDocId);

                var linkedDocument = new LinkedDocument
                {
                    SdocId = message.SourceDocumentId,
                    LinkedSdocId = documentAssessmentData.SdocId,
                    RsdraNumber = documentAssessmentData.SourceName,
                    SourceDocumentName = documentAssessmentData.Name,
                    ReceiptDate = documentAssessmentData.ReceiptDate,
                    SourceDocumentType = documentAssessmentData.DocumentType,
                    SourceNature = documentAssessmentData.SourceName,
                    Datum = documentAssessmentData.Datum,
                    LinkType = "Forward",
                    Status = LinkedDocumentRetrievalStatus.NotAttached.ToString(),
                    Created = DateTime.Now
                };

                _dbContext.Add(linkedDocument);
                _dbContext.SaveChanges();
            }
        }
    }
}
