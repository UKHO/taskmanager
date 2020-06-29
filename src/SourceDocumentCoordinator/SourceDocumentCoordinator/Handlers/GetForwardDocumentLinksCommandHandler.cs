using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

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

            _logger.LogInformation("Forward linked documents found for SourceDocumentId: {SourceDocumentId}; LinkedDocuments: {LinkedDocuments}");

            var failedLinkedDocuments = new StringBuilder(linkedDocIds.Count);

            await PersistLinkedDocuments(message, linkedDocIds, failedLinkedDocuments);

            if (failedLinkedDocuments.Length > 0)
            {
                throw new ApplicationException(
                    $"Errors while adding Backward linked documents to SourceDocumentId: {message.SourceDocumentId}:" +
                    $"{Environment.NewLine}{failedLinkedDocuments}");
            }

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");
        }

        private async Task PersistLinkedDocuments(GetForwardDocumentLinksCommand message, IEnumerable<int> linkedDocIds,
            StringBuilder failedLinkedDocuments)
        {
            foreach (var linkedDocId in linkedDocIds)
            {
                LogContext.PushProperty("LinkedDocument", linkedDocId);

                _logger.LogInformation(
                    "Adding Forward LinkedDocument: {LinkedDocument} to SourceDocumentId: {SourceDocumentId}");

                try
                {
                    var documentAssessmentData = await _dataServiceApiClient.GetAssessmentData(linkedDocId);

                    var linkedDocument = await _dbContext.LinkedDocument.FirstOrDefaultAsync(l =>
                        l.ProcessId == message.ProcessId
                        && l.LinkedSdocId == documentAssessmentData.SdocId
                        && l.LinkType == DocumentLinkType.Forward.ToString());

                    var isNew = linkedDocument == null;

                    if (isNew)
                    {
                        linkedDocument = new LinkedDocument();
                    }

                    linkedDocument.ProcessId = message.ProcessId;
                    linkedDocument.PrimarySdocId = message.SourceDocumentId;
                    linkedDocument.LinkedSdocId = documentAssessmentData.SdocId;
                    linkedDocument.RsdraNumber = documentAssessmentData.SourceName;
                    linkedDocument.SourceDocumentName = documentAssessmentData.Name;
                    linkedDocument.ReceiptDate = documentAssessmentData.ReceiptDate;
                    linkedDocument.SourceDocumentType = documentAssessmentData.DocumentType;
                    linkedDocument.SourceNature = documentAssessmentData.SourceName;
                    linkedDocument.Datum = documentAssessmentData.Datum;
                    linkedDocument.LinkType = DocumentLinkType.Forward.ToString();
                    linkedDocument.Status = SourceDocumentRetrievalStatus.NotAttached.ToString();
                    linkedDocument.Created = DateTime.Now;

                    if (isNew)
                    {
                        await _dbContext.LinkedDocument.AddAsync(linkedDocument);
                    }

                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "Successfully added Forward LinkedDocument: {LinkedDocument} to SourceDocumentId: {SourceDocumentId}");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to add Forward LinkedDocument: {LinkedDocument} to SourceDocumentId: {SourceDocumentId}");

                    failedLinkedDocuments.AppendLine(
                        $"Failed to add Forward LinkedDocument: {linkedDocId} to SourceDocumentId: {message.SourceDocumentId}: {e.Message}");
                }
            }
        }
    }

}
