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
                .FirstOrDefaultAsync(wi => wi.ProcessId == message.ProcessId);

            if (workflowInstance == null)
            {
                _logger.LogError("Unable to find database record for ProcessId {ProcessId}");

                throw new ApplicationException($"Unable to find database record for ProcessId {message.ProcessId}");
            }

            var sdocId = workflowInstance.PrimaryDocumentStatus.SdocId;
            var action =
                workflowInstance.DbAssessmentVerifyData.TaskType.Equals("Simple",
                    StringComparison.InvariantCultureIgnoreCase)
                    ? "Imm Act - NM"
                    : "Longer-term Action";

            if (workflowInstance.PrimaryDocumentStatus.Status == SourceDocumentRetrievalStatus.Completed.ToString())
            {
                // Already marked as Completed
                _logger.LogInformation("SDRA Job with SdocId {sdocId}, belongs to ProcessId {ProcessId}, is already marked as Completed.", sdocId);

                return;
            }

            // Mark SDRA Assessment as Assessed first, then mark as completed
            if (workflowInstance.PrimaryDocumentStatus.Status != SourceDocumentRetrievalStatus.Assessed.ToString() )
            {
                await UpdateSdraAssessmentAsAssessed(sdocId, message.ProcessId, action);
            }

            await UpdateSdraAssessmentAsCompleted(sdocId);
        }

        private async Task UpdateSdraAssessmentAsAssessed(int sdocId, int processId, string action)
        {
            _logger.LogInformation("Marking PrimarySdocIds {SdocId}s as Assessed, triggered by ProcessId {ProcessId}", sdocId);
            
            try
            {
                await _dataServiceApiClient.MarkAssessmentAsAssessed(processId.ToString(), sdocId, action, "tbc");

                // Update all occurrences of this sdocId in PrimaryDocumentStatus with status SourceDocumentRetrievalStatus.Assessed
                var primaryDocuments = await _dbContext.PrimaryDocumentStatus.Where(p => p.SdocId == sdocId).ToListAsync();
                primaryDocuments.ForEach(p => p.Status = SourceDocumentRetrievalStatus.Assessed.ToString());

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully Marked PrimarySdocIds {SdocId} as Assessed, triggered by ProcessId {ProcessId}", sdocId);

            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed MarkAssessmentAsAssessed call to DataService {DataServiceResource} with: PrimarySdocId: {PrimarySdocId}",
                    nameof(_dataServiceApiClient.MarkAssessmentAsAssessed),
                    sdocId);
                throw;
            }
        }

        private async Task UpdateSdraAssessmentAsCompleted(int sdocId)
        {
            _logger.LogInformation("Marking PrimarySdocIds {SdocId}s as Completed, triggered by ProcessId {ProcessId}", sdocId);

            try
            {
                await _dataServiceApiClient.MarkAssessmentAsCompleted(sdocId, "Assessed and Completed by TM2");

                // Update all occurrences of this sdocId in PrimaryDocumentStatus with status SourceDocumentRetrievalStatus.Completed
                var primaryDocuments = await _dbContext.PrimaryDocumentStatus.Where(p => p.SdocId == sdocId).ToListAsync();
                primaryDocuments.ForEach(p => p.Status = SourceDocumentRetrievalStatus.Completed.ToString());

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully Marked PrimarySdocIds {SdocId} as Completed, triggered by ProcessId {ProcessId}", sdocId);

            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed MarkAssessmentAsCompleted call to DataService {DataServiceResource} with: PrimarySdocId: {PrimarySdocId}",
                    nameof(_dataServiceApiClient.MarkAssessmentAsCompleted),
                    sdocId);
                throw;
            }
        }
    }
}
