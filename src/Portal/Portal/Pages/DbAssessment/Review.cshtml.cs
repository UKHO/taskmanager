using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Helpers.Auth;
using Common.Messages.Enums;
using Common.Messages.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly ICommentsHelper _commentsHelper;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<ReviewModel> _logger;
        private readonly IPageValidationHelper _pageValidationHelper;

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
        public string Reviewer { get; set; }

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
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetailsAsync(this.User).Result;
                return _currentUser;
            }
        }

        public ReviewModel(WorkflowDbContext dbContext,
            IWorkflowBusinessLogicService workflowBusinessLogicService,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            ICommentsHelper commentsHelper,
            IAdDirectoryService adDirectoryService,
            ILogger<ReviewModel> logger,
            IPageValidationHelper pageValidationHelper)
        {
            _dbContext = dbContext;
            _workflowBusinessLogicService = workflowBusinessLogicService;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _commentsHelper = commentsHelper;
            _adDirectoryService = adDirectoryService;
            _logger = logger;
            _pageValidationHelper = pageValidationHelper;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;

            IsReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(ProcessId);

            var currentReviewData = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);
            OperatorsModel = await _OperatorsModel.GetOperatorsDataAsync(currentReviewData, _dbContext).ConfigureAwait(false);
            OperatorsModel.ParentPage = WorkflowStage = WorkflowStage.Review;

            SerialisedCustomHttpStatusCodes = EnumHandlers.EnumToString<ReviewCustomHttpStatusCode>();

            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostReviewTerminateAsync(string comment, int processId)
        {
            LogContext.PushProperty("ActivityName", "Review");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostReviewTerminateAsync));
            LogContext.PushProperty("Comment", comment);
            LogContext.PushProperty("UserFullName", CurrentUser.DisplayName);

            _logger.LogInformation("Entering terminate with: ProcessId: {ProcessId}; Comment: {Comment};");

            if (string.IsNullOrWhiteSpace(comment))
            {
                _logger.LogError("Comment is null, empty or whitespace: {Comment}");
                throw new ArgumentException($"{nameof(comment)} is null, empty or whitespace");
            }

            if (processId < 1)
            {
                _logger.LogError("ProcessId is less than 1: {ProcessId}");
                throw new ArgumentException($"{nameof(processId)} is less than 1");
            }

            ProcessId = processId;

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} has already been terminated");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} has already been terminated");
                throw appException;
            }

            _logger.LogInformation("Terminating with: ProcessId: {ProcessId}; Comment: {Comment};");

            try
            {
                await MarkTaskAsTerminated(processId,comment);
            }
            catch (Exception e)
            {
                await MarkWorkflowInstanceAsStarted(processId);

                _logger.LogError("Unable to terminate task {ProcessId}.", e);

                ValidationErrorMessages.Add($"Unable to terminate task. Please retry later: {e.Message}");

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)ReviewCustomHttpStatusCode.FailuresDetected
                };
            }

            return StatusCode((int)HttpStatusCode.OK);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            LogContext.PushProperty("ActivityName", "Review");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("Action", action);
            LogContext.PushProperty("UserFullName", CurrentUser.DisplayName);

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} has already been terminated");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} has already been terminated");
                throw appException;
            }

            _logger.LogInformation("Entering Done with: ProcessId: {ProcessId}; Action: {Action};");

            ValidationErrorMessages.Clear();

            var currentReviewData = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);

            if (!await _pageValidationHelper.CheckReviewPageForErrors(action, PrimaryAssignedTask, AdditionalAssignedTasks, Team, Reviewer, ValidationErrorMessages, CurrentUser.DisplayName, currentReviewData.Reviewer))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)ReviewCustomHttpStatusCode.FailedValidation
                };
            }

            ProcessId = processId;

            PrimaryAssignedTask.ProcessId = ProcessId;

            await UpdateOnHold(ProcessId, IsOnHold);
            await UpdateDbAssessmentReviewData(ProcessId);
            await SaveAdditionalTasks(ProcessId);
            await UpdateAssessmentData(ProcessId);

            if (action == "Done")
            {

                try
                {
                    await MarkTaskAsComplete(processId);
                }
                catch (Exception e)
                {
                    await MarkWorkflowInstanceAsStarted(processId);

                    _logger.LogError("Unable to progress task {ProcessId} from Review to Assess.", e);

                    ValidationErrorMessages.Add($"Unable to progress task from Review to Assess. Please retry later: {e.Message}");

                    return new JsonResult(this.ValidationErrorMessages)
                    {
                        StatusCode = (int)ReviewCustomHttpStatusCode.FailuresDetected
                    };
                }

                // TODO: Move to workflow coordinator
                //    await CopyPrimaryAssignTaskNoteToComments(processId);
                //    await ProcessAdditionalTasks(processId);
                //    await PersistPrimaryTask(processId, workflowInstance);

                //    LogContext.PushProperty("CurrentUser.DisplayName", CurrentUser.DisplayName);
                //    _logger.LogInformation("{CurrentUser.DisplayName} successfully progressed {ActivityName} to Assess on 'Done' button with: ProcessId: {ProcessId}; Action: {Action};");

                //    await _commentsHelper.AddComment($"Review step completed",
                //        processId,
                //        workflowInstance.WorkflowInstanceId,
                //        CurrentUser.DisplayName);
            }
            else
            {
                await _commentsHelper.AddComment($"Review: Changes saved",
                    processId,
                    currentReviewData.WorkflowInstanceId,
                    CurrentUser.DisplayName);
            }

            _logger.LogInformation("Finished Done with: ProcessId: {ProcessId}; Action: {Action};");

            return StatusCode((int)HttpStatusCode.OK);
        }

        public async Task<IActionResult> OnPostValidateTerminateAsync(int processId)
        {
            LogContext.PushProperty("ActivityName", "Review");
            LogContext.PushProperty("ProcessId", processId);

            _logger.LogInformation("Entering ValidateTerminate with: ProcessId: {ProcessId}; Action: {Action};");
            
            await GetOnHoldData(processId);

            if (IsOnHold)
            {
                ValidationErrorMessages.Add("Unable to Terminate task. Take task off hold before terminating.");

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)ReviewCustomHttpStatusCode.FailedValidation
                };
            }

            return StatusCode((int)HttpStatusCode.OK);
        }
        
        private async Task MarkTaskAsComplete(int processId)
        {
            var workflowInstance = await MarkWorkflowInstanceAsUpdating(processId);

            await PublishProgressWorkflowInstanceEvent(processId, workflowInstance, WorkflowStage.Review, WorkflowStage.Assess);
            
            _logger.LogInformation(
                "Task progression from {ActivityName} to Assess has been triggered by {UserFullName} with: ProcessId: {ProcessId}; Action: {Action};");

            await _commentsHelper.AddComment("Task progression from Review to Assess has been triggered",
                processId,
                workflowInstance.WorkflowInstanceId,
                CurrentUser.DisplayName);
        }
        
        private async Task MarkTaskAsTerminated(int processId, string comment)
        {
            var workflowInstance = await MarkWorkflowInstanceAsUpdating(processId);

            await _commentsHelper.AddComment($"Terminate comment: {comment}",
                processId,
                workflowInstance.WorkflowInstanceId,
                CurrentUser.DisplayName);

            await PublishProgressWorkflowInstanceEvent(processId, workflowInstance, WorkflowStage.Review,
                WorkflowStage.Terminated);

            _logger.LogInformation(
                "Task termination from {ActivityName} has been triggered by {UserFullName} with: ProcessId: {ProcessId}; Action: {Action};");

            await _commentsHelper.AddComment("Task termination has been triggered",
                processId,
                workflowInstance.WorkflowInstanceId,
                CurrentUser.DisplayName);
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

        private async Task PersistPrimaryTask(int processId, WorkflowInstance workflowInstance)
        {
            var correlationId = workflowInstance.PrimaryDocumentStatus.CorrelationId;

            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = correlationId.HasValue ? correlationId.Value : Guid.NewGuid(),
                ProcessId = processId,
                FromActivity = WorkflowStage.Review,
                ToActivity = WorkflowStage.Assess
            };

            LogContext.PushProperty("PersistWorkflowInstanceDataEvent",
                persistWorkflowInstanceDataEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
            await _eventServiceApiClient.PostEvent(nameof(PersistWorkflowInstanceDataEvent), persistWorkflowInstanceDataEvent);
            _logger.LogInformation("Published PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
        }


        private async Task SaveAdditionalTasks(int processId)
        {
            _logger.LogInformation("Saving additional task belonging to task with processId: {ProcessId}");

            var toRemove = await _dbContext.DbAssessmentAssignTask.Where(at => at.ProcessId == processId).ToListAsync();
            _dbContext.DbAssessmentAssignTask.RemoveRange(toRemove);

            await _dbContext.SaveChangesAsync();

            foreach (var task in AdditionalAssignedTasks)
            {
                task.ProcessId = processId;
                await _dbContext.DbAssessmentAssignTask.AddAsync(task);
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task ProcessAdditionalTasks(int processId)
        {
            var primaryDocumentStatus = await _dbContext.PrimaryDocumentStatus.FirstAsync(d => d.ProcessId == processId);
            var correlationId = primaryDocumentStatus.CorrelationId.Value;

            foreach (var task in AdditionalAssignedTasks)
            {
                var docRetrievalEvent = new StartWorkflowInstanceEvent
                {
                    CorrelationId = correlationId,
                    WorkflowType = WorkflowType.DbAssessment,
                    ParentProcessId = processId,
                    AssignedTaskId = task.DbAssessmentAssignTaskId
                };

                _logger.LogInformation("Publishing StartWorkflowInstanceEvent: {StartWorkflowInstanceEvent};",
                    docRetrievalEvent.ToJSONSerializedString());
                await _eventServiceApiClient.PostEvent(nameof(StartWorkflowInstanceEvent), docRetrievalEvent);
                _logger.LogInformation("Published StartWorkflowInstanceEvent: {StartWorkflowInstanceEvent};",
                    docRetrievalEvent.ToJSONSerializedString());
            }
        }

        private async Task CopyPrimaryAssignTaskNoteToComments(int processId)
        {
            var primaryAssignTask = await _dbContext.DbAssessmentReviewData
                .FirstOrDefaultAsync(r => r.ProcessId == processId);

            if (!string.IsNullOrEmpty(primaryAssignTask.Notes))
            {
                await _dbContext.Comment.AddAsync(new Comment()
                {
                    ProcessId = processId,
                    WorkflowInstanceId = primaryAssignTask.WorkflowInstanceId,
                    Text = $"Assign Task: {primaryAssignTask.Notes.Trim()}",
                    Username = CurrentUser.DisplayName,
                    Created = DateTime.Today
                });

                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task UpdateDbAssessmentReviewData(int processId)
        {
            var currentReview = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == processId);
            currentReview.Assessor = PrimaryAssignedTask.Assessor;
            currentReview.Verifier = PrimaryAssignedTask.Verifier;
            currentReview.TaskType = PrimaryAssignedTask.TaskType;
            currentReview.Reviewer = Reviewer;
            currentReview.Notes = PrimaryAssignedTask.Notes;
            currentReview.WorkspaceAffected = PrimaryAssignedTask.WorkspaceAffected;
            currentReview.Ion = Ion;
            currentReview.ActivityCode = ActivityCode;
            currentReview.SourceCategory = SourceCategory;

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
                IsOnHold = true;

                var existingOnHoldRecord = await _dbContext.OnHold.FirstOrDefaultAsync(r => r.ProcessId == processId &&
                                                                                                         r.OffHoldTime == null);
                if (existingOnHoldRecord != null)
                {
                    return;
                }

                var workflowInstance = _dbContext.WorkflowInstance
                    .FirstOrDefault(wi => wi.ProcessId == processId);

                var onHoldRecord = new OnHold
                {
                    ProcessId = processId,
                    OnHoldTime = DateTime.Now,
                    OnHoldUser = CurrentUser.DisplayName,
                    WorkflowInstanceId = workflowInstance.WorkflowInstanceId
                };

                await _dbContext.OnHold.AddAsync(onHoldRecord);
                await _dbContext.SaveChangesAsync();

                await _commentsHelper.AddComment($"Task {processId} has been put on hold",
                    processId,
                    workflowInstance.WorkflowInstanceId,
                    CurrentUser.DisplayName);
            }
            else
            {
                IsOnHold = false;

                var existingOnHoldRecord = await _dbContext.OnHold.FirstOrDefaultAsync(r => r.ProcessId == processId &&
                                                                                                     r.OffHoldTime == null);
                if (existingOnHoldRecord == null)
                {
                    return;
                }

                existingOnHoldRecord.OffHoldTime = DateTime.Now;
                existingOnHoldRecord.OffHoldUser = CurrentUser.DisplayName;

                await _dbContext.SaveChangesAsync();

                await _commentsHelper.AddComment($"Task {processId} taken off hold",
                    processId,
                    _dbContext.WorkflowInstance.First(p => p.ProcessId == processId)
                        .WorkflowInstanceId,
                    CurrentUser.DisplayName);
            }
        }

        private async Task<WorkflowInstance> MarkWorkflowInstanceAsUpdating(int processId)
        {
            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(w => w.PrimaryDocumentStatus)
                .FirstAsync(wi => wi.ProcessId == processId);

            workflowInstance.Status = WorkflowStatus.Updating.ToString();
            workflowInstance.ActivityChangedAt = DateTime.Today;
            await _dbContext.SaveChangesAsync();

            return workflowInstance;
        }
        
        private async Task MarkWorkflowInstanceAsStarted(int processId)
        {
            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(w => w.PrimaryDocumentStatus)
                .FirstAsync(w => w.ProcessId == processId);

            workflowInstance.Status = WorkflowStatus.Started.ToString();

            await _dbContext.SaveChangesAsync();
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}