using System;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.Handlers
{
    public class ProgressWorkflowInstanceEventHandler : IHandleMessages<ProgressWorkflowInstanceEvent>,
                                                            IHandleMessages<PersistWorkflowInstanceDataCommand>,
                                                            IHandleMessages<PersistWorkflowInstanceDataEvent>
    {
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly ILogger<ProgressWorkflowInstanceEventHandler> _logger;
        private readonly WorkflowDbContext _dbContext;

        public ProgressWorkflowInstanceEventHandler(IWorkflowServiceApiClient workflowServiceApiClient,
            ILogger<ProgressWorkflowInstanceEventHandler> logger, WorkflowDbContext dbContext)
        {
            _workflowServiceApiClient = workflowServiceApiClient;
            _logger = logger;
            _dbContext = dbContext;
        }

        // TODO: Duplicate PersistWorkflowInstanceDataEvent to a command fired by new event ProgressWorkflowInstanceEvent
        // TODO: eventually PersistWorkflowInstanceDataEvent and its handler will me removed (when Review, Assess, and Verify are completed)

        public async Task Handle(ProgressWorkflowInstanceEvent message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(ProgressWorkflowInstanceEvent));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("FromActivity", message.FromActivity);
            LogContext.PushProperty("ToActivity", message.ToActivity);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var k2Task = await _workflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId);

            switch (message.ToActivity)
            {
                case WorkflowStage.Assess:
                    // TODO: Added code for Verifying, progressing and persisting
                    throw new NotImplementedException("Progressing Review to Assess, or Rejecting Verify to Ass has not been implemented");

                    //break;
                case WorkflowStage.Verify:
                    if (k2Task.ActivityName == WorkflowStage.Assess.ToString())
                    {
                        // progress
                        var success = await _workflowServiceApiClient.ProgressWorkflowInstance(message.ProcessId, k2Task.SerialNumber);
                        
                    }
                    else if (k2Task.ActivityName == WorkflowStage.Review.ToString())
                    {
                        LogContext.PushProperty("K2Stage", k2Task.ActivityName);
                        _logger.LogError("K2Task with ProcessId {ProcessId} is at K2 stage {K2Stage} and not at {ToActivity}, while moving task from {FromActivity}");
                        throw new ApplicationException($"K2Task with ProcessId {message.ProcessId} is at K2 stage {k2Task.ActivityName} and not at {message.ToActivity}, while moving task from {message.FromActivity}");
                    }

                    // fire persist command
                    var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
                    {
                        CorrelationId = message.CorrelationId,
                        ProcessId = message.ProcessId,
                        FromActivity = message.FromActivity,
                        ToActivity = message.ToActivity
                    };

                    await context.SendLocal(persistWorkflowInstanceDataCommand).ConfigureAwait(false);

                    break;
                case WorkflowStage.Completed:
                    // TODO: Added code for Verifying, progressing and persisting
                    throw new NotImplementedException($"{message.ToActivity} has not been implemented for processId: {message.ProcessId}.");

                    //break;
                default:
                    throw new NotImplementedException($"{message.ToActivity} has not been implemented for processId: {message.ProcessId}.");
            }

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");
        }


        public async Task Handle(PersistWorkflowInstanceDataCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(PersistWorkflowInstanceDataCommand));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("FromActivity", message.FromActivity);
            LogContext.PushProperty("ToActivity", message.ToActivity);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");
            
            var k2Task = await _workflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId);

            WorkflowInstance workflowInstance = null;
            
            switch (message.ToActivity)
            {
                case WorkflowStage.Assess:

                    ValidateK2TaskForAssessAndVerify(message, k2Task);

                    workflowInstance = await UpdateWorkflowInstanceData(message.ProcessId, message.ToActivity, k2Task);

                    var isRejected = message.FromActivity == WorkflowStage.Verify;

                    if (isRejected)
                    {
                        await PersistWorkflowDataToAssessFromVerify(message.ProcessId, message.FromActivity, workflowInstance);
                    }
                    else
                    {
                        await PersistWorkflowDataToAssessFromReview(message.ProcessId, message.FromActivity, workflowInstance);
                    }

                    break;
                case WorkflowStage.Verify:

                    ValidateK2TaskForAssessAndVerify(message, k2Task);

                    workflowInstance = await UpdateWorkflowInstanceData(message.ProcessId, message.ToActivity, k2Task);

                    await PersistWorkflowDataToVerifyFromAssess(message.ProcessId, workflowInstance.WorkflowInstanceId);

                    break;
                case WorkflowStage.Completed:

                    ValidateK2TaskForSignOff(message, k2Task);

                    await UpdateWorkflowInstanceData(message.ProcessId, message.ToActivity, k2Task);

                    // Fire CompleteAssessmentCommand to mark SDRA Assessment as completed
                    var completeAssessment = new CompleteAssessmentCommand
                    {
                        CorrelationId = message.CorrelationId,
                        ProcessId = message.ProcessId
                    };

                    await context.SendLocal(completeAssessment).ConfigureAwait(false);

                    _logger.LogInformation("Task with processId: {ProcessId} has been completed.");

                    break;
                default:
                    throw new NotImplementedException($"{message.ToActivity} has not been implemented for processId: {message.ProcessId}.");
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");

        }

        public async Task Handle(PersistWorkflowInstanceDataEvent message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("MessageId", context.MessageId);
            LogContext.PushProperty("Message", message.ToJSONSerializedString());
            LogContext.PushProperty("EventName", nameof(PersistWorkflowInstanceDataEvent));
            LogContext.PushProperty("MessageCorrelationId", message.CorrelationId);
            LogContext.PushProperty("ProcessId", message.ProcessId);
            LogContext.PushProperty("FromActivity", message.FromActivity);
            LogContext.PushProperty("ToActivity", message.ToActivity);

            _logger.LogInformation("Entering {EventName} handler with: {Message}");

            var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
            {
                CorrelationId = message.CorrelationId,
                ProcessId = message.ProcessId,
                FromActivity = message.FromActivity,
                ToActivity = message.ToActivity
            };

            await context.SendLocal(persistWorkflowInstanceDataCommand).ConfigureAwait(false);

            _logger.LogInformation("Successfully Completed Event {EventName}: {Message}");

        }

        private void ValidateK2TaskForSignOff(PersistWorkflowInstanceDataCommand message, K2TaskData k2Task)
        {
            if (k2Task != null)
            {
                // if task is at Completed, then we expect k2Task to be null
                _logger.LogError("K2 Task is not at expected stage {ToActivity} for ProcessId {ProcessId}; but was at " +
                                 k2Task.ActivityName);
                throw new ApplicationException(
                    $"K2 Task is not at expected stage {message.ToActivity} for ProcessId {message.ProcessId}; but was at {k2Task.ActivityName}");
            }
        }

        private void ValidateK2TaskForAssessAndVerify(PersistWorkflowInstanceDataCommand message, K2TaskData k2Task)
        {
            if (k2Task == null)
            {
                _logger.LogError("Failed to get data for K2 Task with ProcessId {ProcessId} while moving task from {FromActivity} to {ToActivity}");
                throw new ApplicationException($"Failed to get data for K2 Task with ProcessId {message.ProcessId} while moving task from {message.FromActivity} to {message.ToActivity}");
            }

            if (k2Task.ActivityName != message.ToActivity.ToString())
            {
                LogContext.PushProperty("K2Stage", k2Task.ActivityName);
                _logger.LogError("K2Task with ProcessId {ProcessId} is at K2 stage {K2Stage} and not at {ToActivity}, while moving task from {FromActivity}");
                throw new ApplicationException($"K2Task with ProcessId {message.ProcessId} is at K2 stage {k2Task.ActivityName} and not at {message.ToActivity}, while moving task from {message.FromActivity}");
            }
        }

        private async Task<WorkflowInstance> UpdateWorkflowInstanceData(int processId, WorkflowStage toActivity, K2TaskData k2Task)
        {
            var workflowInstance = await _dbContext.WorkflowInstance.Include(wi => wi.ProductAction)
                .Include(wi => wi.DataImpact)
                .FirstAsync(wi => wi.ProcessId == processId);

            workflowInstance.SerialNumber = (toActivity == WorkflowStage.Completed) ? "" : k2Task.SerialNumber;
            workflowInstance.ActivityName = (toActivity == WorkflowStage.Completed) ? WorkflowStage.Verify.ToString() : k2Task.ActivityName;

            workflowInstance.Status = (toActivity == WorkflowStage.Completed) ? WorkflowStatus.Completed.ToString() : WorkflowStatus.Started.ToString();
            workflowInstance.ActivityChangedAt = DateTime.Today;

            return workflowInstance;

        }

        private async Task PersistWorkflowDataToAssessFromReview(int processId, WorkflowStage fromActivity, WorkflowInstance workflowInstance)
        {
            LogContext.PushProperty("PersistWorkflowDataToAssessFromReview", nameof(PersistWorkflowDataToAssessFromReview));
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("WorkflowInstanceId", workflowInstance.WorkflowInstanceId);
            LogContext.PushProperty("FromActivity", fromActivity);

            _logger.LogInformation("Entering {PersistWorkflowDataToAssessFromReview} with processId: {ProcessId}; " +
                                   "workflowInstanceId: {WorkflowInstanceId}; and FromActivity: {FromActivity}.");


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
            assessData.WorkflowInstanceId = workflowInstance.WorkflowInstanceId;

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

        private async Task PersistWorkflowDataToAssessFromVerify(int processId, WorkflowStage fromActivity, WorkflowInstance workflowInstance)
        {
            LogContext.PushProperty("PersistWorkflowDataToAssessFromVerify", nameof(PersistWorkflowDataToAssessFromVerify));
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("WorkflowInstanceId", workflowInstance.WorkflowInstanceId);
            LogContext.PushProperty("FromActivity", fromActivity);

            _logger.LogInformation("Entering {PersistWorkflowDataToAssessFromVerify} with processId: {ProcessId}; " +
                                   "workflowInstanceId: {WorkflowInstanceId}; and FromActivity: {FromActivity}.");


            foreach (var productAction in workflowInstance.ProductAction)
            {
                productAction.Verified = false;
            }

            foreach (var dataImpact in workflowInstance.DataImpact)
            {
                dataImpact.FeaturesSubmitted = false;
                dataImpact.FeaturesVerified = false;
            }

            var verifyData = await _dbContext.DbAssessmentVerifyData.SingleAsync(d => d.ProcessId == processId);

            _logger.LogInformation("Saving primary task data from verify to assess for processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");

            var assessData =
                await _dbContext.DbAssessmentAssessData.SingleOrDefaultAsync(d => d.ProcessId == processId);

            var isExists = (assessData != null);

            if (!isExists)
            {
                assessData = new DbAssessmentAssessData();
            }

            assessData.ProcessId = processId;
            assessData.WorkflowInstanceId = workflowInstance.WorkflowInstanceId;

            assessData.ActivityCode = verifyData.ActivityCode;
            assessData.Ion = verifyData.Ion;
            assessData.SourceCategory = verifyData.SourceCategory;
            assessData.WorkspaceAffected = verifyData.WorkspaceAffected;
            assessData.TaskType = verifyData.TaskType;
            assessData.Reviewer = verifyData.Reviewer;
            assessData.Assessor = verifyData.Assessor;
            assessData.Verifier = verifyData.Verifier;

            if (!isExists)
            {
                await _dbContext.DbAssessmentAssessData.AddAsync(assessData);
            }
        }


        private async Task PersistWorkflowDataToVerifyFromAssess(
            int processId,
            int workflowInstanceId)
        {
            LogContext.PushProperty("PersistWorkflowDataToVerifyFromAssess", nameof(PersistWorkflowDataToVerifyFromAssess));
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("WorkflowInstanceId", workflowInstanceId);

            _logger.LogInformation("Entering {PersistWorkflowDataToVerifyFromAssess} with processId: {ProcessId} and workflowInstanceId: {WorkflowInstanceId}.");

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
