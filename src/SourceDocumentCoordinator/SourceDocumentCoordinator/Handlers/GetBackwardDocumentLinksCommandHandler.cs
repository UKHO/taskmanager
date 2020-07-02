using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using DataServices.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;
using LinkedDocument = WorkflowDatabase.EF.Models.LinkedDocument;

namespace SourceDocumentCoordinator.Handlers
{
    public class GetBackwardDocumentLinksCommandHandler : IHandleMessages<GetBackwardDocumentLinksCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly ILogger<GetBackwardDocumentLinksCommandHandler> _logger;
        private readonly WorkflowDbContext _dbContext;

        public GetBackwardDocumentLinksCommandHandler(
                                                        WorkflowDbContext dbContext,
                                                        IDataServiceApiClient dataServiceApiClient,
                                                        ILogger<GetBackwardDocumentLinksCommandHandler> logger)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Handle(GetBackwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(GetBackwardDocumentLinksCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("SourceDocumentId", message.SourceDocumentId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var docLinks = await _dataServiceApiClient.GetBackwardDocumentLinks(message.SourceDocumentId);

            if (docLinks == null || docLinks.Count == 0)
            {
                _logger.LogInformation("No Backward linked documents found for SourceDocumentId: {SourceDocumentId}");

                return;
            }

            var linkedDocIds = docLinks.Select(s => s.DocId1).ToList(); //Backward Linked will have DocId2 as sdocId, while DocId1 as LinkedSdocId

            LogContext.PushProperty("LinkedDocuments", linkedDocIds.ToJSONSerializedString());

            _logger.LogInformation("Backward linked documents found for Source Document: {SourceDocumentId}; Linked Documents: {LinkedDocuments}");

            var failedLinkedDocuments = new StringBuilder(linkedDocIds.Count);

            foreach (var linkedDocId in linkedDocIds)
            {

                LogContext.PushProperty("LinkedDocument", linkedDocId);

                _logger.LogInformation(
                    "Processing backward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");


                var linkedDocumentAssessmentData =
                    await GetLinkedDocumentAssessmentData(linkedDocId, failedLinkedDocuments);

                await PersistLinkedDocuments(message, linkedDocId, linkedDocumentAssessmentData, failedLinkedDocuments);
            }

            await _dbContext.SaveChangesAsync();

            if (failedLinkedDocuments.Length > 0)
            {
                throw new ApplicationException(
                    $"Errors while adding Backward linked documents to Source Document: {message.SourceDocumentId}:" +
                            $"{Environment.NewLine}{failedLinkedDocuments}");
            }

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");

        }

        private async Task<DocumentAssessmentData> GetLinkedDocumentAssessmentData(int linkedDocId, StringBuilder failedLinkedDocuments)
        {
            DocumentAssessmentData documentAssessmentData = null;
            try
            {
                documentAssessmentData = await _dataServiceApiClient.GetAssessmentData(linkedDocId);
                
                _logger.LogInformation(
                    "Successfully retrieved assessment data for backward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get assessment data for backward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");

                failedLinkedDocuments.AppendLine(
                    $"Failed to get assessment data for backward linked document: {linkedDocId}: {e.Message}");
            }

            return documentAssessmentData;
        }

        private async Task PersistLinkedDocuments(GetBackwardDocumentLinksCommand message, int linkedDocId, DocumentAssessmentData documentAssessmentData, StringBuilder failedLinkedDocuments)
        {
            try
            {
                var linkedDocument = await _dbContext.LinkedDocument.FirstOrDefaultAsync(l =>
                                                                            l.ProcessId == message.ProcessId
                                                                            && l.LinkedSdocId == documentAssessmentData.SdocId
                                                                            && l.LinkType == DocumentLinkType.Backward.ToString());

                var isNew = linkedDocument == null;

                if (isNew)
                {
                    linkedDocument = new LinkedDocument();
                }

                linkedDocument.ProcessId = message.ProcessId;
                linkedDocument.PrimarySdocId = message.SourceDocumentId;
                linkedDocument.LinkedSdocId = linkedDocId;
                linkedDocument.LinkType = DocumentLinkType.Backward.ToString();
                linkedDocument.Status = SourceDocumentRetrievalStatus.NotAttached.ToString();
                linkedDocument.Created = DateTime.Now;

                if (documentAssessmentData != null)
                {
                    linkedDocument.RsdraNumber = documentAssessmentData.SourceName;
                    linkedDocument.SourceDocumentName = documentAssessmentData.Name;
                    linkedDocument.ReceiptDate = documentAssessmentData.ReceiptDate;
                    linkedDocument.SourceDocumentType = documentAssessmentData.DocumentType;
                    linkedDocument.SourceNature = documentAssessmentData.SourceName;
                    linkedDocument.Datum = documentAssessmentData.Datum;
                }

                if (isNew)
                {
                    await _dbContext.LinkedDocument.AddAsync(linkedDocument);
                }

                _logger.LogInformation(
                    "Successfully added backward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add backward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");

                failedLinkedDocuments.AppendLine(
                    $"Failed to add backward linked document: {linkedDocId}: {e.Message}");
            }
        }
    }
}
