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
    public class GetSepDocumentLinksCommandHandler : IHandleMessages<GetSepDocumentLinksCommand>
    {
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly ILogger<GetSepDocumentLinksCommandHandler> _logger;
        private readonly WorkflowDbContext _dbContext;

        public GetSepDocumentLinksCommandHandler(
            WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            ILogger<GetSepDocumentLinksCommandHandler> logger)
        {
            _dataServiceApiClient = dataServiceApiClient;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Handle(GetSepDocumentLinksCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(GetSepDocumentLinksCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("SourceDocumentId", message.SourceDocumentId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var docLinks = await _dataServiceApiClient.GetSepDocumentLinks(message.SourceDocumentId);

            if (docLinks == null || docLinks.Count == 0)
            {
                _logger.LogInformation("No SEP linked documents found for SourceDocumentId: {SourceDocumentId}");

                return;
            }

            var linkedDocIds = docLinks.Select(s => s.Id).ToList();

            LogContext.PushProperty("LinkedDocuments", linkedDocIds.ToJSONSerializedString());

            _logger.LogInformation("SEP linked documents found for SourceDocumentId: {SourceDocumentId}; LinkedDocuments: {LinkedDocuments}");

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

        private async Task PersistLinkedDocuments(GetSepDocumentLinksCommand message, IEnumerable<int> linkedDocIds,
            StringBuilder failedLinkedDocuments)
        {
            foreach (var linkedDocId in linkedDocIds)
            {
                LogContext.PushProperty("LinkedDocument", linkedDocId);

                _logger.LogInformation(
                    "Adding SEP LinkedDocument: {LinkedDocument} to SourceDocumentId: {SourceDocumentId}");

                try
                {
                    var documentAssessmentData = await _dataServiceApiClient.GetAssessmentData(linkedDocId);

                    var linkedDocument = await _dbContext.LinkedDocument.FirstOrDefaultAsync(l =>
                        l.ProcessId == message.ProcessId
                        && l.LinkedSdocId == documentAssessmentData.SdocId
                        && l.LinkType == DocumentLinkType.Sep.ToString());

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
                    linkedDocument.LinkType = DocumentLinkType.Sep.ToString();
                    linkedDocument.Status = LinkedDocumentRetrievalStatus.NotAttached.ToString();
                    linkedDocument.Created = DateTime.Now;

                    if (isNew)
                    {
                        await _dbContext.LinkedDocument.AddAsync(linkedDocument);
                    }

                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "Successfully added SEP LinkedDocument: {LinkedDocument} to SourceDocumentId: {SourceDocumentId}");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to add SEP LinkedDocument: {LinkedDocument} to SourceDocumentId: {SourceDocumentId}");

                    failedLinkedDocuments.AppendLine(
                        $"Failed to add SEP LinkedDocument: {linkedDocId} to SourceDocumentId: {message.SourceDocumentId}: {e.Message}");
                }
            }
        }
    }
}
