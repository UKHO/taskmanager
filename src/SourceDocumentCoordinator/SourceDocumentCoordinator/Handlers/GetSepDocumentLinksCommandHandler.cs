using System;
using System.Threading.Tasks;
using Common.Messages.Commands;
using NServiceBus;
using SourceDocumentCoordinator.HttpClients;
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

            if (docLinks?.Count == 0) return;

            foreach (var documentObject in docLinks)
            {
                var documentAssessmentData = await _dataServiceApiClient.GetAssessmentData(documentObject.Id);

                var linkedDocument = new LinkedDocument
                {
                    ProcessId = message.ProcessId,
                    PrimarySdocId = message.SourceDocumentId,
                    LinkedSdocId = documentAssessmentData.SdocId,
                    RsdraNumber = documentAssessmentData.SourceName,
                    SourceDocumentName = documentAssessmentData.Name,
                    ReceiptDate = documentAssessmentData.ReceiptDate,
                    SourceDocumentType = documentAssessmentData.DocumentType,
                    SourceNature = documentAssessmentData.SourceName,
                    Datum = documentAssessmentData.Datum,
                    LinkType = "SEP",
                    Status = LinkedDocumentRetrievalStatus.NotAttached.ToString(),
                    Created = DateTime.Now
                };

                _dbContext.Add(linkedDocument);
                _dbContext.SaveChanges();
            }
        }
    }
}
