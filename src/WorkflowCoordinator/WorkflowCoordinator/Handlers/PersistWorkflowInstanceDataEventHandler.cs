using Common.Helpers;
using Common.Messages.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCoordinator.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.Handlers
{
    public class PersistWorkflowInstanceDataEventHandler : IHandleMessages<PersistWorkflowInstanceDataEvent>
    {
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly ILogger<PersistWorkflowInstanceDataEventHandler> _logger;
        private readonly WorkflowDbContext _dbContext;

        public PersistWorkflowInstanceDataEventHandler(IWorkflowServiceApiClient workflowServiceApiClient,
            ILogger<PersistWorkflowInstanceDataEventHandler> logger, WorkflowDbContext dbContext)
        {
            _workflowServiceApiClient = workflowServiceApiClient;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task Handle(PersistWorkflowInstanceDataEvent message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(PersistWorkflowInstanceDataEvent));
            LogContext.PushProperty("CorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("FromActivity", message.FromActivity);
            LogContext.PushProperty("ToActivity", message.ToActivity);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var k2Task = await _workflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId);

            if (message.ToActivityName == "Completed" && k2Task != null)
            {
                // if task as at Completed, then we expect k2Task to be null
                _logger.LogError("Failed to get data for K2 Task at stage with ProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get data for K2 Task at stage with ProcessId {message.ProcessId}");
            }


            if (k2Task == null && message.ToActivity != WorkflowStage.Completed)
            {
                _logger.LogError("Failed to get data for K2 Task at stage with ProcessId {ProcessId}");
                throw new ApplicationException($"Failed to get data for K2 Task at stage with ProcessId {message.ProcessId}");
            }

            if (message.ToActivity != WorkflowStage.Completed && k2Task.ActivityName != message.ToActivity.ToString())
            {
                LogContext.PushProperty("K2Stage", k2Task.ActivityName);
                _logger.LogError("K2Task at stage {K2Stage} is not at {ToActivity}");
                throw new ApplicationException($"K2Task at stage {k2Task.ActivityName} is not at {message.ToActivity}");
            }

            var workflowInstance = await _dbContext.WorkflowInstance.Include(wi => wi.ProductAction)
                .Include(wi => wi.DataImpact)
                .FirstAsync(wi => wi.ProcessId == message.ProcessId);


            //if (message.FromActivity == WorkflowStage.Verify && message.ToActivity == WorkflowStage.Assess)

            switch (message.ToActivity)
            {
                case WorkflowStage.Assess:
                    workflowInstance.SerialNumber = k2Task.SerialNumber;
                    workflowInstance.ActivityName = k2Task.ActivityName;

                    workflowInstance.Status = WorkflowStatus.Started.ToString();

                    await PersistWorkflowDataToAssess(message.ProcessId, workflowInstance.WorkflowInstanceId, message.FromActivity, workflowInstance);
                    break;
                case WorkflowStage.Verify:
                    workflowInstance.SerialNumber = k2Task.SerialNumber;
                    workflowInstance.ActivityName = k2Task.ActivityName;

                    workflowInstance.Status = WorkflowStatus.Started.ToString();

                    await PersistWorkflowDataToVerify(message.ProcessId, workflowInstance.WorkflowInstanceId);
                    break;
                case WorkflowStage.Completed:
                    workflowInstance.SerialNumber = "";
                    workflowInstance.ActivityName = WorkflowStatus.Completed.ToString();

                    workflowInstance.Status = WorkflowStatus.Completed.ToString();

                    _logger.LogInformation("Task with processId: {ProcessId} has been completed.");
                    break;
                default:
                    throw new NotImplementedException($"{message.ToActivity} has not been implemented for processId: {message.ProcessId}.");
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");

        }

        private async Task PersistWorkflowDataToAssess(int processId,
            int workflowInstanceId, WorkflowStage fromActivity, WorkflowInstance workflowInstance)
        {
            LogContext.PushProperty("PersistWorkflowDataToAssess", nameof(PersistWorkflowDataToAssess));
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("WorkflowInstanceId", workflowInstanceId);
            LogContext.PushProperty("FromActivity", fromActivity);

            _logger.LogInformation("Entering {PersistWorkflowDataToAssess} with processId: {ProcessId}; " +
                                   "workflowInstanceId: {WorkflowInstanceId}; and FromActivity: {FromActivity}.");

            if (fromActivity == WorkflowStage.Verify)
            {
                //TODO: Copy data from Verify to Assess

                foreach (var productAction in workflowInstance.ProductAction)
                {
                    productAction.Verified = false;
                }
                foreach (var dataImpact in workflowInstance.DataImpact)
                {
                    dataImpact.Verified = false;
                }

                return;
            }

            var reviewData = await _dbContext.DbAssessmentReviewData.SingleAsync(d => d.ProcessId == processId);

            _logger.LogInformation("Saving primary task data from review to assess for processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");

            var assessData =
                await _dbContext.DbAssessmentAssessData.SingleOrDefaultAsync(d => d.ProcessId == processId);

            var isExists = (assessData != null);

            if (!isExists)
            {
                assessData = new DbAssessmentAssessData();
            }

            assessData.ProcessId = processId;
            assessData.WorkflowInstanceId = workflowInstanceId;

            assessData.ActivityCode = reviewData.ActivityCode;
            assessData.Ion = reviewData.Ion;
            assessData.SourceCategory = reviewData.SourceCategory;
            assessData.WorkspaceAffected = reviewData.WorkspaceAffected;
            assessData.TaskType = reviewData.TaskType;
            assessData.Reviewer = reviewData.Reviewer;
            assessData.Assessor = reviewData.Assessor;
            assessData.Verifier = reviewData.Verifier;

            if (!isExists)
            {
                await _dbContext.DbAssessmentAssessData.AddAsync(assessData);
            }
        }


        private async Task PersistWorkflowDataToVerify(
            int processId,
            int workflowInstanceId)
        {
            LogContext.PushProperty("PersistWorkflowDataToVerify", nameof(PersistWorkflowDataToVerify));
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("WorkflowInstanceId", workflowInstanceId);

            _logger.LogInformation("Entering {PersistWorkflowDataToVerify} with processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");

            var assessData = await _dbContext.DbAssessmentAssessData.SingleAsync(d => d.ProcessId == processId);

            _logger.LogInformation("Saving task data from assess to verify for processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");


            var verifyData = await _dbContext.DbAssessmentVerifyData.SingleOrDefaultAsync(d => d.ProcessId == processId);

            var isExists = (verifyData != null);
            if (!isExists)
            {
                verifyData = new DbAssessmentVerifyData();
            }

            verifyData.ProcessId = processId;
            verifyData.WorkflowInstanceId = workflowInstanceId;

            verifyData.ActivityCode = assessData.ActivityCode;
            verifyData.Ion = assessData.Ion;
            verifyData.SourceCategory = assessData.SourceCategory;
            verifyData.WorkspaceAffected = assessData.WorkspaceAffected;
            verifyData.TaskType = assessData.TaskType;
            verifyData.ProductActioned = assessData.ProductActioned;
            verifyData.ProductActionChangeDetails = assessData.ProductActionChangeDetails;
            verifyData.Reviewer = assessData.Reviewer;
            verifyData.Assessor = assessData.Assessor;
            verifyData.Verifier = assessData.Verifier;

            if (!isExists)
            {
                await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);
            }
        }
    }
}
