using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Messages.Enums;
using Common.Messages.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Portal.Auth;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class ReviewModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly ICommentsHelper _commentsHelper;
        private readonly IUserIdentityService _userIdentityService;

        public int ProcessId { get; set; }
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

        public List<string> ValidationErrorMessages { get; set; }

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public ReviewModel(WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            ICommentsHelper commentsHelper,
            IUserIdentityService userIdentityService)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _commentsHelper = commentsHelper;
            _userIdentityService = userIdentityService;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostReviewTerminateAsync(string comment, int processId)
        {
            //TODO: Log!

            if (string.IsNullOrWhiteSpace(comment))
            {
                //TODO: Log error!
                throw new ArgumentException($"{nameof(comment)} is null, empty or whitespace");
            }

            if (processId < 1)
            {
                //TODO: Log error!
                throw new ArgumentException($"{nameof(processId)} is less than 1");
            }

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            var workflowInstance = UpdateWorkflowInstanceAsTerminated(processId);
            await _commentsHelper.AddComment($"Terminate comment: {comment}",
                processId,
                workflowInstance.WorkflowInstanceId,
                UserFullName);
            await UpdateK2WorkflowAsTerminated(workflowInstance);
            await UpdateSdraAssessmentAsCompleted(comment, workflowInstance);

            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            //TODO: Log
            ValidationErrorMessages.Clear();
            var isValid = true;

            // Show error to user where we have an invalid source type
            if (!ValidateSourceType())
            {
                isValid = false;
            }

            if (!ValidateWorkspace())
            {
                isValid = false;
            }

            if (!ValidateUsers())
            {
                isValid = false;
            }

            if (!isValid)
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            ProcessId = processId;

            PrimaryAssignedTask.ProcessId = ProcessId;

            await UpdateDbAssessmentReviewData();
            await UpdateAdditionalAssignTasks(processId);

            await _dbContext.SaveChangesAsync();

            if (action == "Done")
            {
                var primaryDocumentStatus = await _dbContext.PrimaryDocumentStatus.FirstAsync(d => d.ProcessId == processId);
                var correlationId = primaryDocumentStatus.CorrelationId.Value;

                await CopyPrimaryAssignTaskNoteToComments(processId);

                for (var i = 1; i < AdditionalAssignedTasks.Count; i++)
                {
                    //TODO: Log
                    //TODO: Must validate incoming models
                    //TODO: Copy Additional Assign Task Notes to new child Assess Comments;
                    //TODO:          using the new K2 ProcessId; hence implement it in StartWorkflowInstanceEvent handler
                    var docRetrievalEvent = new StartWorkflowInstanceEvent
                    {
                        CorrelationId = correlationId,
                        WorkflowType = WorkflowType.DbAssessment,
                        ParentProcessId = processId
                    };
                    //await _eventServiceApiClient.PostEvent(nameof(StartWorkflowInstanceEvent), docRetrievalEvent);
                }
            }

            return StatusCode((int)HttpStatusCode.OK);
        }

        private async Task CopyPrimaryAssignTaskNoteToComments(int processId)
        {
            var primaryAssignTask = await _dbContext.DbAssessmentReviewData
                .FirstOrDefaultAsync(r => r.ProcessId == processId);

            if (!string.IsNullOrEmpty(primaryAssignTask.Notes))
            {
                UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

                await _dbContext.Comment.AddAsync(new Comments()
                {
                    ProcessId = processId,
                    WorkflowInstanceId = primaryAssignTask.WorkflowInstanceId,
                    Text = $"Assign Task: {primaryAssignTask.Notes.Trim()}",
                    Username = UserFullName,
                    Created = DateTime.Today
                });

                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task UpdateDbAssessmentReviewData()
        {
            var currentReview = await _dbContext.DbAssessmentReviewData.FirstAsync(r => r.ProcessId == ProcessId);
            currentReview.Assessor = PrimaryAssignedTask.Assessor;
            currentReview.Verifier = PrimaryAssignedTask.Verifier;
            currentReview.AssignedTaskSourceType = PrimaryAssignedTask.AssignedTaskSourceType;
            currentReview.Notes = PrimaryAssignedTask.Notes;
            currentReview.WorkspaceAffected = PrimaryAssignedTask.WorkspaceAffected;
            currentReview.Ion = Ion;
            currentReview.ActivityCode = ActivityCode;
            currentReview.SourceCategory = SourceCategory;
        }

        private async Task UpdateAdditionalAssignTasks(int processId)
        {
            var toRemove = await _dbContext.DbAssessmentAssignTask.Where(at => at.ProcessId == processId).ToListAsync();
            _dbContext.DbAssessmentAssignTask.RemoveRange(toRemove);

            foreach (var task in AdditionalAssignedTasks)
            {
                task.ProcessId = processId;
                await _dbContext.DbAssessmentAssignTask.AddAsync(task);
            }
        }

        private bool ValidateSourceType()
        {
            if (string.IsNullOrEmpty(PrimaryAssignedTask.AssignedTaskSourceType))
            {
                ValidationErrorMessages.Add($"Assign Task 1: Source Type is required");
                return false;
            }

            if (!_dbContext.AssignedTaskSourceType.Any(st => st.Name == PrimaryAssignedTask.AssignedTaskSourceType))
            {
                ValidationErrorMessages.Add($"Assign Task 1: Source Type {PrimaryAssignedTask.AssignedTaskSourceType} does not exist");
                return false;
            }

            var sourceTypes = AdditionalAssignedTasks.Select(st => st.AssignedTaskSourceType).ToList();

            if (sourceTypes.Any(s => string.IsNullOrEmpty(s)))
            {
                ValidationErrorMessages.Add($"Additional Assign Task: Source Type is required");
                return false;
            }

            var erroneousEntries = sourceTypes.Except(_dbContext.AssignedTaskSourceType.Select(st => st.Name));
            if (erroneousEntries.Any())
            {
                var entry = string.Join(',', erroneousEntries);
                ValidationErrorMessages.Add($"Additional Assign Task: Invalid Source Type - {entry}");
                return false;
            }

            return true;
        }

        private bool ValidateWorkspace()
        {
            if (string.IsNullOrEmpty(PrimaryAssignedTask.WorkspaceAffected))
            {
                ValidationErrorMessages.Add($"Assign Task 1: Workspace Affected is required");
                return false;
            }

            var workspaceAffected = AdditionalAssignedTasks.Select(st => st.WorkspaceAffected).ToList();

            if (workspaceAffected.Any(s => string.IsNullOrEmpty(s)))
            {
                ValidationErrorMessages.Add($"Additional Assign Task: Workspace Affected is required");
                return false;
            }

            return true;
        }

        private bool ValidateUsers()
        {
            if (string.IsNullOrEmpty(PrimaryAssignedTask.Assessor))
            {
                ValidationErrorMessages.Add($"Assign Task 1: Assessor is required");
                return false;
            }

            var assessor = AdditionalAssignedTasks.Select(st => st.Assessor).ToList();

            if (assessor.Any(s => string.IsNullOrEmpty(s)))
            {
                ValidationErrorMessages.Add($"Additional Assign Task: Assessor is required");
                return false;
            }

            return true;
        }


        private async Task UpdateSdraAssessmentAsCompleted(string comment, WorkflowInstance workflowInstance)
        {
            try
            {
                await _dataServiceApiClient.PutAssessmentCompleted(workflowInstance.AssessmentData.PrimarySdocId, comment);
            }
            catch (Exception e)
            {
                //TODO: Log error!
            }
        }

        private async Task UpdateK2WorkflowAsTerminated(WorkflowInstance workflowInstance)
        {
            try
            {
                await _workflowServiceApiClient.TerminateWorkflowInstance(workflowInstance.SerialNumber);
            }
            catch (Exception e)
            {
                //TODO: Log error!
            }
        }

        private WorkflowInstance UpdateWorkflowInstanceAsTerminated(int processId)
        {
            var workflowInstance = _dbContext.WorkflowInstance
                .Include(wi => wi.AssessmentData)
                .FirstOrDefault(wi => wi.ProcessId == processId);

            if (workflowInstance == null)
            {
                //TODO: Log error!
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the WorkflowInstance table");
            }

            workflowInstance.Status = WorkflowStatus.Terminated.ToString();
            _dbContext.SaveChanges();

            return workflowInstance;
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}