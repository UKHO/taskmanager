using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.Handlers
{
    public class CompleteAssessmentCommandHandler : IHandleMessages<CompleteAssessmentCommand>
    {
        private readonly ILogger<CompleteAssessmentCommandHandler> _logger;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly WorkflowDbContext _dbContext;

        public CompleteAssessmentCommandHandler(
                                                ILogger<CompleteAssessmentCommandHandler> logger,
                                                IDataServiceApiClient dataServiceApiClient,
                                                WorkflowDbContext dbContext)
        {
            _logger = logger;
            _dataServiceApiClient = dataServiceApiClient;
            _dbContext = dbContext;
        }

        public async Task Handle(CompleteAssessmentCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(CompleteAssessmentCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(wi => wi.DbAssessmentVerifyData)
                .Include(wi => wi.PrimaryDocumentStatus)
                .Include(w => w.Comments)
                .AsNoTracking()
                .FirstOrDefaultAsync(wi => wi.ProcessId == message.ProcessId);

            if (workflowInstance == null)
            {
                _logger.LogError("Unable to find database record for ProcessId {ProcessId}");

                throw new ApplicationException($"Unable to find database record for ProcessId {message.ProcessId}");
            }

            LogContext.PushProperty("SdocId", workflowInstance.PrimaryDocumentStatus.SdocId);

            if (workflowInstance.PrimaryDocumentStatus.Status == SourceDocumentRetrievalStatus.Completed.ToString())
            {
                // Already marked as Completed
                _logger.LogInformation("SDRA Job with SdocId {SdocId}, belongs to ProcessId {ProcessId}, is already marked as Completed.");

                return;
            }

            // Mark SDRA Assessment as Assessed first, then mark as completed
            if (workflowInstance.Status != WorkflowStatus.Terminated.ToString() && workflowInstance.PrimaryDocumentStatus.Status != SourceDocumentRetrievalStatus.Assessed.ToString())
            {
                await UpdateSdraAssessmentAsAssessed(workflowInstance);
            }

            await UpdateSdraAssessmentAsCompleted(workflowInstance);
        }

        private async Task UpdateSdraAssessmentAsAssessed(WorkflowInstance workflowInstance)
        {
            _logger.LogInformation("Marking PrimarySdocIds {SdocId} as Assessed, triggered by ProcessId {ProcessId}");

            try
            {
                var sdocId = workflowInstance.PrimaryDocumentStatus.SdocId;
                var action = workflowInstance.DbAssessmentVerifyData.TaskType=="Simple" ? "Imm Act - NM" : "Longer-term Action";
                var change = string.IsNullOrWhiteSpace(workflowInstance.DbAssessmentVerifyData.ProductActionChangeDetails) ? "n/a" : workflowInstance.DbAssessmentVerifyData.ProductActionChangeDetails;

                await _dataServiceApiClient.MarkAssessmentAsAssessed(workflowInstance.ProcessId.ToString(), sdocId, action, change);

                // Update all occurrences of this sdocId in PrimaryDocumentStatus with status SourceDocumentRetrievalStatus.Assessed
                var primaryDocuments = await _dbContext.PrimaryDocumentStatus.Where(p => p.SdocId == sdocId).ToListAsync();
                primaryDocuments.ForEach(p => p.Status = SourceDocumentRetrievalStatus.Assessed.ToString());

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully Marked PrimarySdocIds {SdocId} as Assessed, triggered by ProcessId {ProcessId}");

            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed MarkAssessmentAsAssessed call to DataService for PrimarySdocId: {SdocId}");
                throw;
            }
        }

        private async Task UpdateSdraAssessmentAsCompleted(WorkflowInstance workflowInstance)
        {
            _logger.LogInformation("Marking PrimarySdocIds {SdocId} as Completed, triggered by ProcessId {ProcessId}");

            try
            {
                var sdocId = workflowInstance.PrimaryDocumentStatus.SdocId;

                var terminateComment = workflowInstance.Comments.FirstOrDefault(c => c.Text.Contains("Terminate"));
                var comment = (terminateComment == null || string.IsNullOrWhiteSpace(terminateComment.Text)) ? "Marked Completed via TM2" : terminateComment.Text;

                await _dataServiceApiClient.MarkAssessmentAsCompleted(sdocId, comment);

                // Update all occurrences of this sdocId in PrimaryDocumentStatus with status SourceDocumentRetrievalStatus.Completed
                var primaryDocuments = await _dbContext.PrimaryDocumentStatus.Where(p => p.SdocId == sdocId).ToListAsync();
                primaryDocuments.ForEach(p => p.Status = SourceDocumentRetrievalStatus.Completed.ToString());

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully Marked PrimarySdocIds {SdocId} as Completed, triggered by ProcessId {ProcessId}");

            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed MarkAssessmentAsCompleted call to DataService for PrimarySdocId: {SdocId}");
                throw;
            }
        }
    }
}
