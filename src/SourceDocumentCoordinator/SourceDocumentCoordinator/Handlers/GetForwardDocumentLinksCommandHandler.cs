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
    public class GetForwardDocumentLinksCommandHandler : IHandleMessages<GetForwardDocumentLinksCommand>
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly ILogger<GetForwardDocumentLinksCommandHandler> _logger;

        public GetForwardDocumentLinksCommandHandler(
            WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            ILogger<GetForwardDocumentLinksCommandHandler> logger)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _logger = logger;
        }

        public async Task Handle(GetForwardDocumentLinksCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(GetForwardDocumentLinksCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("SourceDocumentId", message.SourceDocumentId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var docLinks = await _dataServiceApiClient.GetForwardDocumentLinks(message.SourceDocumentId);

            if (docLinks == null || docLinks.Count == 0)
            {
                _logger.LogInformation("No Forward linked documents found for SourceDocumentId: {SourceDocumentId}");

                return;
            }

            var linkedDocIds = docLinks.Select(s => s.DocId2).ToList();

            LogContext.PushProperty("LinkedDocuments", linkedDocIds.ToJSONSerializedString());

            _logger.LogInformation("Forward linked documents found for Source Document: {SourceDocumentId}; Linked Documents: {LinkedDocuments}");

            var failedLinkedDocuments = new StringBuilder(linkedDocIds.Count);

            foreach (var linkedDocId in linkedDocIds)
            {

                LogContext.PushProperty("LinkedDocument", linkedDocId);

                _logger.LogInformation(
                    "Processing forward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");


                var linkedDocumentAssessmentData =
                    await GetLinkedDocumentAssessmentData(linkedDocId, failedLinkedDocuments);

                await PersistLinkedDocuments(message, linkedDocId, linkedDocumentAssessmentData, failedLinkedDocuments);
            }
            
            await _dbContext.SaveChangesAsync();

            if (failedLinkedDocuments.Length > 0)
            {
                throw new ApplicationException(
                    $"Errors while adding Forward linked documents to SourceDocumentId: {message.SourceDocumentId}:" +
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
                    "Successfully retrieved assessment data for forward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get assessment data for forward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");

                failedLinkedDocuments.AppendLine(
                    $"Failed to get assessment data for forward linked document: {linkedDocId}: {e.Message}");
            }

            return documentAssessmentData;
        }

        private async Task PersistLinkedDocuments(GetForwardDocumentLinksCommand message, int linkedDocId, DocumentAssessmentData documentAssessmentData, StringBuilder failedLinkedDocuments)
        {
            try
            {
                var linkedDocument = await _dbContext.LinkedDocument.FirstOrDefaultAsync(l =>
                                                                            l.ProcessId == message.ProcessId
                                                                            && l.LinkedSdocId == linkedDocId
                                                                            && l.LinkType == DocumentLinkType.Forward.ToString());

                var isNew = linkedDocument == null;

                if (isNew)
                {
                    linkedDocument = new LinkedDocument();
                }

                linkedDocument.ProcessId = message.ProcessId;
                linkedDocument.PrimarySdocId = message.SourceDocumentId;
                linkedDocument.LinkedSdocId = linkedDocId;
                linkedDocument.LinkType = DocumentLinkType.Forward.ToString();
                linkedDocument.Status = SourceDocumentRetrievalStatus.NotAttached.ToString();
                linkedDocument.Created = DateTime.Now;
                linkedDocument.UniqueId = Guid.NewGuid();

                if (documentAssessmentData != null)
                {
                    linkedDocument.RsdraNumber = documentAssessmentData.SourceName;
                    linkedDocument.SourceDocumentName = documentAssessmentData.Name;
                    linkedDocument.ReceiptDate = documentAssessmentData.ReceiptDate;
                    linkedDocument.SourceDocumentType = documentAssessmentData.DocumentType;
                    linkedDocument.SourceNature = documentAssessmentData.DocumentNature;
                    linkedDocument.Datum = documentAssessmentData.Datum;
                }

                if (isNew)
                {
                    await _dbContext.LinkedDocument.AddAsync(linkedDocument);
                }

                LogContext.PushProperty("UniqueId", linkedDocument.UniqueId);
                _logger.LogInformation(
                    "Successfully added forward linked document: {LinkedDocument} to Source Document: {SourceDocumentId} with UniqueId: {UniqueId}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add forward linked document: {LinkedDocument} to Source Document: {SourceDocumentId}");

                failedLinkedDocuments.AppendLine(
                    $"Failed to add forward linked document: {linkedDocId}: {e.Message}");
            }
        }
    }

}
