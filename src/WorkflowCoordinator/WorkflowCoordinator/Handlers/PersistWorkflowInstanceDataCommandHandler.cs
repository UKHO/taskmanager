using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Enums;
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
    public class PersistWorkflowInstanceDataCommandHandler : IHandleMessages<PersistWorkflowInstanceDataCommand>,
                                                            IHandleMessages<PersistWorkflowInstanceDataEvent>
    {
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly ILogger<PersistWorkflowInstanceDataCommandHandler> _logger;
        private readonly WorkflowDbContext _dbContext;

        public PersistWorkflowInstanceDataCommandHandler(IWorkflowServiceApiClient workflowServiceApiClient,
            ILogger<PersistWorkflowInstanceDataCommandHandler> logger, WorkflowDbContext dbContext)
        {
            _workflowServiceApiClient = workflowServiceApiClient;
            _logger = logger;
            _dbContext = dbContext;
        }

        // TODO: eventually PersistWorkflowInstanceDataEvent and its handler will me removed (when Review, Assess, and Verify are completed)
        
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
            CompleteAssessmentCommand completeAssessment = null;


            switch (message.ToActivity)
            {
                case WorkflowStage.Terminated:

                    ValidateK2TaskForTerminationOrSignOff(message, k2Task);

                    await UpdateWorkflowInstanceData(message.ProcessId, string.Empty, WorkflowStage.Review, WorkflowStatus.Terminated);

                    // Fire CompleteAssessmentCommand to mark SDRA Assessment as completed but not Assessed
                    completeAssessment = new CompleteAssessmentCommand
                    {
                        CorrelationId = message.CorrelationId,
                        ProcessId = message.ProcessId
                    };

                    await context.SendLocal(completeAssessment).ConfigureAwait(false);

                    _logger.LogInformation("Task with processId: {ProcessId} has been Terminated.");

                    break;
                case WorkflowStage.Assess:

                    ValidateK2TaskForAssessAndVerify(message, k2Task);

                    workflowInstance = await UpdateWorkflowInstanceData(message.ProcessId, k2Task.SerialNumber, WorkflowStage.Assess, WorkflowStatus.Started);

                    var isRejected = message.FromActivity == WorkflowStage.Verify;

                    if (isRejected)
                    {
                        // Verify to Assess
                        await PersistWorkflowDataToAssessFromVerify(message.ProcessId, message.FromActivity, workflowInstance);
                    }
                    else
                    {
                        // Review to Assess

                        await CopyPrimaryAssignTaskNoteToComments(message.ProcessId);
                        await PersistWorkflowDataToAssessFromReview(message.ProcessId, message.FromActivity, workflowInstance);
                        await ProcessAdditionalTasks(message, context);
                    }

                    break;
                case WorkflowStage.Verify:
                    // Assess to Verify
                    ValidateK2TaskForAssessAndVerify(message, k2Task);

                    workflowInstance = await UpdateWorkflowInstanceData(message.ProcessId, k2Task.SerialNumber, WorkflowStage.Verify, WorkflowStatus.Started);

                    await PersistWorkflowDataToVerifyFromAssess(message.ProcessId, workflowInstance.WorkflowInstanceId);

                    break;
                case WorkflowStage.Completed:
                    // Verify Signed off

                    ValidateK2TaskForTerminationOrSignOff(message, k2Task);

                    await UpdateWorkflowInstanceData(message.ProcessId, string.Empty, WorkflowStage.Verify, WorkflowStatus.Completed);

                    // Fire CompleteAssessmentCommand to mark SDRA Assessment as Assessed and Completed
                    completeAssessment = new CompleteAssessmentCommand
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
            // TODO: eventually PersistWorkflowInstanceDataEvent and its handler will me removed (when Review, Assess, and Verify are completed)

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

        private void ValidateK2TaskForTerminationOrSignOff(PersistWorkflowInstanceDataCommand message, K2TaskData k2Task)
        {
            if (k2Task != null)
            {
                // if task is at Terminated or Completed, then we expect k2Task to be null
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

        private async Task<WorkflowInstance> UpdateWorkflowInstanceData(int processId, string serialNumber, WorkflowStage activityName, WorkflowStatus status)
        {
            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(wi => wi.ProductAction)
                .Include(wi => wi.DataImpact)
                .FirstAsync(wi => wi.ProcessId == processId);

            workflowInstance.SerialNumber = serialNumber; //(toActivity == WorkflowStage.Completed) ? "" : k2Task.SerialNumber;
            workflowInstance.ActivityName = activityName.ToString(); //(toActivity == WorkflowStage.Completed) ? WorkflowStage.Verify.ToString() : k2Task.ActivityName;

            workflowInstance.Status = status.ToString(); //(toActivity == WorkflowStage.Completed) ? WorkflowStatus.Completed.ToString() : WorkflowStatus.Started.ToString();
            workflowInstance.ActivityChangedAt = DateTime.Today;

            return workflowInstance;

        }

        private async Task PersistWorkflowDataToAssessFromReview(int processId, WorkflowStage fromActivity, WorkflowInstance workflowInstance)
        {
            LogContext.PushProperty("PersistWorkflowDataToAssessFromReview", nameof(PersistWorkflowDataToAssessFromReview));
            LogContext.PushProperty("WorkflowInstanceId", workflowInstance.WorkflowInstanceId);

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
            LogContext.PushProperty("WorkflowInstanceId", workflowInstance.WorkflowInstanceId);

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
        
        private async Task CopyPrimaryAssignTaskNoteToComments(int processId)
        {
            LogContext.PushProperty("CopyPrimaryAssignTaskNoteToComments", nameof(CopyPrimaryAssignTaskNoteToComments));

            _logger.LogInformation("Entering {CopyPrimaryAssignTaskNoteToComments} with processId: {ProcessId}.");

            var primaryAssignTask = await _dbContext.DbAssessmentReviewData
                .FirstOrDefaultAsync(r => r.ProcessId == processId);

            if (!string.IsNullOrEmpty(primaryAssignTask.Notes))
            {
                await _dbContext.Comment.AddAsync(new Comment()
                {
                    ProcessId = processId,
                    WorkflowInstanceId = primaryAssignTask.WorkflowInstanceId,
                    Text = $"Assign Task: {primaryAssignTask.Notes.Trim()}",
                    Username = primaryAssignTask.Reviewer,
                    Created = DateTime.Today
                });

                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task ProcessAdditionalTasks(PersistWorkflowInstanceDataCommand message, IMessageHandlerContext context)
        {
            LogContext.PushProperty("ProcessAdditionalTasks", nameof(ProcessAdditionalTasks));

            _logger.LogInformation("Entering {ProcessAdditionalTasks} with processId: {ProcessId}.");

            var additionalAssignedTasks = await _dbContext.DbAssessmentAssignTask.Where(at => 
                                                            at.ProcessId == message.ProcessId 
                                                            && at.Status == AssignTaskStatus.New.ToString()).ToListAsync();

            foreach (var task in additionalAssignedTasks)
            {
                var docRetrievalEvent = new StartChildWorkflowInstanceCommand
                {
                    CorrelationId = message.CorrelationId,
                    WorkflowType = WorkflowType.DbAssessment,
                    ParentProcessId = message.ProcessId,
                    AssignedTaskId = task.DbAssessmentAssignTaskId
                };

                _logger.LogInformation("Publishing StartChildWorkflowInstanceCommand: {StartChildWorkflowInstanceCommand};",
                    docRetrievalEvent.ToJSONSerializedString());
                await context.SendLocal(docRetrievalEvent).ConfigureAwait(false);
                _logger.LogInformation("Published StartChildWorkflowInstanceCommand: {StartChildWorkflowInstanceCommand};",
                    docRetrievalEvent.ToJSONSerializedString());
                task.Status = AssignTaskStatus.Started.ToString();
                await _dbContext.SaveChangesAsync();
            }
        }

    }
}
