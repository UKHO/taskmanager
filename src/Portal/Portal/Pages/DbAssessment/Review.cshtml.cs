using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Helpers.Auth;
using Common.Messages.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portal.Auth;
using Portal.BusinessLogic;
using Portal.Extensions;
using Portal.Helpers;
using Portal.HttpClients;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    [Authorize]
    public class ReviewModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IWorkflowBusinessLogicService _workflowBusinessLogicService;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly ICommentsHelper _dbAssessmentCommentsHelper;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<ReviewModel> _logger;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly IPortalUserDbService _portalUserDbService;

        public int ProcessId { get; set; }

        public bool IsReadOnly { get; set; }

        [BindProperty]
        public bool IsOnHold { get; set; }

        [BindProperty]
        public string Ion { get; set; }

        [BindProperty]
        public string ActivityCode { get; set; }

        [BindProperty]
        public string SourceCategory { get; set; }

        [BindProperty]
        public DbAssessmentReviewData PrimaryAssignedTask { get; set; }

        [BindProperty]
        public List<DbAssessmentAssignTask> AdditionalAssignedTasks { get; set; }

        [BindProperty]
        public AdUser Reviewer { get; set; }

        [BindProperty]
        public string Team { get; set; }

        public _OperatorsModel OperatorsModel { get; set; }

        public WorkflowStage WorkflowStage { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        public string SerialisedCustomHttpStatusCodes { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;
        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        public ReviewModel(WorkflowDbContext dbContext,
            IWorkflowBusinessLogicService workflowBusinessLogicService,
            IEventServiceApiClient eventServiceApiClient,
            ICommentsHelper dbAssessmentCommentsHelper,
            IAdDirectoryService adDirectoryService,
            ILogger<ReviewModel> logger,
            IPageValidationHelper pageValidationHelper,
            IPortalUserDbService portalUserDbService)
        {
            _dbContext = dbContext;
            _workflowBusinessLogicService = workflowBusinessLogicService;
            _eventServiceApiClient = eventServiceApiClient;
            _dbAssessmentCommentsHelper = dbAssessmentCommentsHelper;
            _adDirectoryService = adDirectoryService;
            _logger = logger;
            _pageValidationHelper = pageValidationHelper;
            _portalUserDbService = portalUserDbService;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;

            LogContext.PushProperty("ActivityName", "Review");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostTerminateAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            _logger.LogInformation("Entering OnGet for {ActivityName} with: ProcessId: {ProcessId}");

            IsReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(ProcessId);

            var currentReviewData = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);
            OperatorsModel = await _OperatorsModel.GetOperatorsDataAsync(currentReviewData, _dbContext).ConfigureAwait(false);
            OperatorsModel.ParentPage = WorkflowStage = WorkflowStage.Review;

            SerialisedCustomHttpStatusCodes = EnumHandlers.EnumToString<ReviewCustomHttpStatusCode>();

            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostTerminateAsync(string comment, int processId)
        {
            LogContext.PushProperty("ActivityName", "Review");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostTerminateAsync));
            LogContext.PushProperty("Comment", comment);
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            _logger.LogInformation("Entering terminate with: ProcessId: {ProcessId}; Comment: {Comment};");

            if (!await _portalUserDbService.ValidateUserAsync(CurrentUser.UserPrincipalName))
            {
                ValidationErrorMessages.Add(
                    "Operators: Your user account is not in the correct authorised group. Please contact system administrators");
                _logger.LogError(
                    "Unable to Terminate task with: ProcessId: {ProcessId} because current user {UserPrincipalName} is not in authorised");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 403 };
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                _logger.LogError("Comment is null, empty or whitespace: {Comment}");
                ValidationErrorMessages.Add("Comment cannot by empty");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            if (processId < 1)
            {
                _logger.LogError("ProcessId is less than 1: {ProcessId}");
                ValidationErrorMessages.Add("Process ID is less than 1");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            ProcessId = processId;
            await GetOnHoldData(processId);

            if (await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId))
            {
                ValidationErrorMessages.Add($"Workflow for process ID {processId} has already been terminated");
                _logger.LogWarning("Workflow Instance for ProcessId {ProcessId} has already been terminated");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            var isAssignedToUser = await _dbContext.DbAssessmentReviewData.AnyAsync(r =>
                r.ProcessId == processId && r.Reviewer.UserPrincipalName == CurrentUser.UserPrincipalName);
            if (!isAssignedToUser)
            {
                ValidationErrorMessages.Add("Operators: You are not assigned as the Reviewer of this task. Please assign the task to yourself and click Save");
                _logger.LogWarning("Unable to Terminate task with: ProcessId: {ProcessId} because it is not assigned to current user");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            if (IsOnHold)
            {
                ValidationErrorMessages.Add("Task Information: Unable to Terminate task. Take task off hold before terminating and click Save.");
                _logger.LogWarning("Unable to Terminate task with: ProcessId: {ProcessId} because it is on hold");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            _logger.LogInformation("Terminating with: ProcessId: {ProcessId}; Comment: {Comment};");

            try
            {
                await MarkTaskAsTerminated(processId, comment);
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to terminate task {ProcessId}.", e);

                await MarkWorkflowInstanceAsStarted(processId);

                ValidationErrorMessages.Add($"Unable to terminate task. Please retry later.");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 500 };
            }

            return new OkResult();
        }


        public async Task<IActionResult> OnPostSaveAsync(int processId)
        {
            LogContext.PushProperty("ActivityName", "Review");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostSaveAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            var action = "Save";
            LogContext.PushProperty("Action", action);

            _logger.LogInformation("Entering Save with: ProcessId: {ProcessId}; Action: {Action};");

            if (!await _portalUserDbService.ValidateUserAsync(CurrentUser.UserPrincipalName))
            {
                ValidationErrorMessages.Add(
                    "Operators: Your user account is not in the correct authorised group. Please contact system administrators");
                _logger.LogInformation(
                    "Unable to Save task with: ProcessId: {ProcessId} because current user {UserPrincipalName} is not authorised");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 403 };
            }

            if (await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId))
            {
                ValidationErrorMessages.Add($"Workflow for process ID {processId} has already been terminated.");
                _logger.LogInformation("Workflow Instance for ProcessId {ProcessId} has already been terminated");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            var currentReviewData = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);
            if (!await _pageValidationHelper.CheckReviewPageForErrors(action, PrimaryAssignedTask,
                AdditionalAssignedTasks, Team, Reviewer, ValidationErrorMessages, CurrentUser.UserPrincipalName,
                currentReviewData.Reviewer))
            {
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            PrimaryAssignedTask.ProcessId = ProcessId = processId;

            try
            {
                await UpdateOnHold(ProcessId, IsOnHold);
                await UpdateDbAssessmentReviewData(ProcessId);
                await SaveAdditionalTasks(ProcessId);
                await UpdateAssessmentData(ProcessId);
            }
            catch (Exception e)
            {
                _logger.LogError("Error updating records for ProcessId {ProcessId}", e);
                ValidationErrorMessages.Add($"System error while updating records for process ID {processId}. Please try again later.");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 500 };
            }

            await _dbAssessmentCommentsHelper.AddComment($"Review: Changes saved",
                processId,
                currentReviewData.WorkflowInstanceId,
                CurrentUser.UserPrincipalName);

            _logger.LogInformation("Finished Save with: ProcessId: {ProcessId}; Action: {Action};");

            return new OkResult();
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId)
        {
            LogContext.PushProperty("ActivityName", "Review");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            var action = "Done";
            LogContext.PushProperty("Action", action);

            _logger.LogInformation("Entering {PortalResource} with ProcessId: {ProcessId}; Action: {Action};");

            if (!await _portalUserDbService.ValidateUserAsync(CurrentUser.UserPrincipalName))
            {
                ValidationErrorMessages.Add(
                    "Operators: Your user account is not in the correct authorised group. Please contact system administrators");
                _logger.LogInformation(
                    "Unable to Complete task with: ProcessId: {ProcessId} because current user {UserPrincipalName} is not in authorised");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 403 };
            }

            if (await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId))
            {
                ValidationErrorMessages.Add($"Workflow for process ID {processId} has already been terminated");
                _logger.LogInformation("Workflow Instance for ProcessId {ProcessId} has already been terminated");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            var currentReviewData = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);
            if (!await _pageValidationHelper.CheckReviewPageForErrors(action,
                PrimaryAssignedTask,
                AdditionalAssignedTasks,
                Team,
                Reviewer,
                ValidationErrorMessages,
                CurrentUser.UserPrincipalName,
                currentReviewData.Reviewer))
            {
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 400 };
            }

            PrimaryAssignedTask.ProcessId = ProcessId = processId;

            try
            {
                await MarkTaskAsComplete(processId);
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to progress task {ProcessId} from Review to Assess.", e);

                await MarkWorkflowInstanceAsStarted(processId);

                ValidationErrorMessages.Add($"Unable to progress task from Review to Assess. Please retry later.");
                return new JsonResult(this.ValidationErrorMessages) { StatusCode = 500 };
            }

            _logger.LogInformation("Finished {PortalResource} with: ProcessId: {ProcessId}; Action: {Action};");

            return new OkResult();
        }

        private async Task MarkTaskAsComplete(int processId)
        {
            var workflowInstance = await MarkWorkflowInstanceAsUpdating(processId);

            await PublishProgressWorkflowInstanceEvent(processId, workflowInstance, WorkflowStage.Review, WorkflowStage.Assess);

            _logger.LogInformation(
                "Task progression from {ActivityName} to Assess has been triggered by {UserPrincipalName} with: ProcessId: {ProcessId}; Action: {Action};");

            await _dbAssessmentCommentsHelper.AddComment("Task progression from Review to Assess has been triggered",
                processId,
                workflowInstance.WorkflowInstanceId,
                CurrentUser.UserPrincipalName);
        }

        private async Task MarkTaskAsTerminated(int processId, string comment)
        {
            var workflowInstance = await MarkWorkflowInstanceAsUpdating(processId);

            await _dbAssessmentCommentsHelper.AddComment($"Terminate comment: {comment}",
                processId,
                workflowInstance.WorkflowInstanceId,
                CurrentUser.UserPrincipalName);

            await PublishProgressWorkflowInstanceEvent(processId, workflowInstance, WorkflowStage.Review,
                WorkflowStage.Terminated);

            _logger.LogInformation(
                "Task termination from {ActivityName} has been triggered by {UserPrincipalName} with: ProcessId: {ProcessId}; Action: {Action};");

            await _dbAssessmentCommentsHelper.AddComment("Task termination has been triggered",
                processId,
                workflowInstance.WorkflowInstanceId,
                CurrentUser.UserPrincipalName);
        }

        private async Task PublishProgressWorkflowInstanceEvent(int processId, WorkflowInstance workflowInstance, WorkflowStage fromActivity, WorkflowStage toActivity)
        {
            var correlationId = workflowInstance.PrimaryDocumentStatus.CorrelationId;

            var progressWorkflowInstanceEvent = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = correlationId.HasValue ? correlationId.Value : Guid.NewGuid(),
                ProcessId = processId,
                FromActivity = fromActivity,
                ToActivity = toActivity
            };

            LogContext.PushProperty("ProgressWorkflowInstanceEvent",
                progressWorkflowInstanceEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing ProgressWorkflowInstanceEvent: {ProgressWorkflowInstanceEvent};");
            await _eventServiceApiClient.PostEvent(nameof(ProgressWorkflowInstanceEvent), progressWorkflowInstanceEvent);
            _logger.LogInformation("Published ProgressWorkflowInstanceEvent: {ProgressWorkflowInstanceEvent};");
        }

        private async Task SaveAdditionalTasks(int processId)
        {
            _logger.LogInformation("Saving additional tasks belonging to task with processId: {ProcessId}...");

            var toRemove = await _dbContext.DbAssessmentAssignTask.Where(at => at.ProcessId == processId).ToListAsync();
            _dbContext.DbAssessmentAssignTask.RemoveRange(toRemove);

            await _dbContext.SaveChangesAsync();

            foreach (var task in AdditionalAssignedTasks)
            {
                LogContext.PushProperty("AdditionalAssignedTaskProcessId", task.ProcessId);
                LogContext.PushProperty("AdditionalAssignedWorkspaceAffected", task.WorkspaceAffected);

                _logger.LogInformation(
                    "Saving additional task with processId {AdditionalAssignedTaskProcessId} " +
                    "and workspace affected: {AdditionalAssignedWorkspaceAffected}, " +
                    "belonging to task with processId {ProcessId} ");

                task.ProcessId = processId;
                task.Assessor = await _portalUserDbService.GetAdUserAsync(task.Assessor.UserPrincipalName);
                task.Verifier = await _portalUserDbService.GetAdUserAsync(task.Verifier.UserPrincipalName);
                task.Status = AssignTaskStatus.New.ToString();
                await _dbContext.DbAssessmentAssignTask.AddAsync(task);
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task UpdateDbAssessmentReviewData(int processId)
        {
            var currentReview = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);

            currentReview.Assessor =
                await _portalUserDbService.ValidateUserAsync(PrimaryAssignedTask.Assessor?.UserPrincipalName)
                    ? await _portalUserDbService.GetAdUserAsync(PrimaryAssignedTask.Assessor?.UserPrincipalName)
                    : null;
            currentReview.Verifier =
                await _portalUserDbService.ValidateUserAsync(PrimaryAssignedTask.Verifier?.UserPrincipalName) ?
                await _portalUserDbService.GetAdUserAsync(PrimaryAssignedTask.Verifier?.UserPrincipalName)
                    : null;
            currentReview.TaskType = PrimaryAssignedTask.TaskType;
            currentReview.Reviewer =
                await _portalUserDbService.ValidateUserAsync(Reviewer?.UserPrincipalName)
                    ? await _portalUserDbService.GetAdUserAsync(Reviewer?.UserPrincipalName)
                    : null;

            currentReview.Notes = PrimaryAssignedTask.Notes;
            currentReview.WorkspaceAffected = PrimaryAssignedTask.WorkspaceAffected;
            currentReview.Ion = Ion;
            currentReview.ActivityCode = ActivityCode;
            currentReview.SourceCategory = SourceCategory;

            _dbContext.DbAssessmentReviewData.Update(currentReview);

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateAssessmentData(int processId)
        {
            var currentAssessment = await _dbContext.AssessmentData.FirstAsync(r => r.ProcessId == processId);
            currentAssessment.TeamDistributedTo = Team;

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateOnHold(int processId, bool onHold)
        {
            if (onHold)
            {
                _logger.LogInformation("Setting workflow instance on hold for ProcessId: {ProcessId} at Action: {Action};");

                IsOnHold = true;

                var existingOnHoldRecord = await _dbContext.OnHold.FirstOrDefaultAsync(r => r.ProcessId == processId &&
                                                                                                         r.OffHoldTime == null);
                if (existingOnHoldRecord != null)
                {
                    _logger.LogWarning("Existing on hold record already exists for ProcessId: {ProcessId}");
                    return;
                }

                var workflowInstance = _dbContext.WorkflowInstance
                    .FirstOrDefault(wi => wi.ProcessId == processId);
                if (workflowInstance is null)
                {
                    _logger.LogWarning("No workflow instance found for ProcessId: {ProcessId}");
                    return;
                }

                var onHoldRecord = new OnHold
                {
                    ProcessId = processId,
                    OnHoldTime = DateTime.Now,
                    OnHoldBy = await _portalUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName),
                    WorkflowInstanceId = workflowInstance.WorkflowInstanceId
                };

                await _dbContext.OnHold.AddAsync(onHoldRecord);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully put task on hold for ProcessId: {ProcessId} at Action: {Action};");

                await _dbAssessmentCommentsHelper.AddComment($"Task {processId} has been put on hold",
                    processId,
                    workflowInstance.WorkflowInstanceId,
                    CurrentUser.UserPrincipalName);
            }
            else
            {
                _logger.LogInformation("Taking workflow instance off hold for ProcessId: {ProcessId} at Action: {Action}");

                IsOnHold = false;

                var existingOnHoldRecord = await _dbContext.OnHold.FirstOrDefaultAsync(r => r.ProcessId == processId &&
                                                                                                     r.OffHoldTime == null);
                if (existingOnHoldRecord == null)
                {
                    _logger.LogWarning("No existing on hold record found for ProcessId: {ProcessId}");
                    return;
                }

                existingOnHoldRecord.OffHoldTime = DateTime.Now;
                existingOnHoldRecord.OffHoldBy = await _portalUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully took task off hold for ProcessId: {ProcessId} at Action: {Action}");

                await _dbAssessmentCommentsHelper.AddComment($"Task {processId} taken off hold",
                    processId,
                    _dbContext.WorkflowInstance.First(p => p.ProcessId == processId)
                        .WorkflowInstanceId,
                    CurrentUser.UserPrincipalName);
            }
        }

        private async Task<WorkflowInstance> MarkWorkflowInstanceAsUpdating(int processId)
        {
            _logger.LogInformation("Marking workflow instance as Updating for ProcessId: {ProcessId} at Action: {Action}");

            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(w => w.PrimaryDocumentStatus)
                .FirstAsync(wi => wi.ProcessId == processId);

            workflowInstance.Status = WorkflowStatus.Updating.ToString();
            workflowInstance.ActivityChangedAt = DateTime.Today;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully marked workflow instance as Updating for ProcessId: {ProcessId} at Action: {Action}");

            return workflowInstance;
        }

        private async Task MarkWorkflowInstanceAsStarted(int processId)
        {
            _logger.LogInformation("Marking workflow instance as Started for ProcessId: {ProcessId} at Action: {Action}");

            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(w => w.PrimaryDocumentStatus)
                .FirstAsync(w => w.ProcessId == processId);

            workflowInstance.Status = WorkflowStatus.Started.ToString();

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully marked workflow instance as Started for ProcessId: {ProcessId} at Action: {Action}");
        }

        private async Task GetOnHoldData(int processId)
        {
            IsOnHold = await _dbContext.OnHold.Where(r => r.ProcessId == processId).AnyAsync(r => r.OffHoldTime == null);
        }
    }
}