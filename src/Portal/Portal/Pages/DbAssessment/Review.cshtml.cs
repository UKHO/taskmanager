using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Enums;
using Common.Messages.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public DbAssessmentReviewData PrimaryAssignedTask { get; set; }
        [BindProperty]
        public List<DbAssessmentAssignTask> AdditionalAssignedTasks { get; set; }

        public List<_AssignTaskModel> AssignTaskModel { get; set; }

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

        public async Task<IActionResult> OnPostDoneAsync(int processId)
        {
            // Work out how many additional Assign Task partials we have, and send a StartWorkflowInstanceEvent for each one
            //TODO: Log

            var correlationId = _dbContext.PrimaryDocumentStatus.First(d => d.ProcessId == processId).CorrelationId.Value;

            //var primaryAssignTaskModel = AssignTaskModel.ElementAt(0);

            //var dbAssessmentReviewData = await _dbContext.DbAssessmentReviewData.FirstAsync(at => at.ProcessId == processId);

            //dbAssessmentReviewData.Assessor = primaryAssignTaskModel.Assessor != null ? primaryAssignTaskModel.Assessor.Name : "";
            //dbAssessmentReviewData.Verifier = primaryAssignTaskModel.Verifier != null ? primaryAssignTaskModel.Verifier.Name : "";
            //dbAssessmentReviewData.Notes = primaryAssignTaskModel.Notes;
            //dbAssessmentReviewData.WorkspaceAffected = primaryAssignTaskModel.WorkspaceAffected;
            //dbAssessmentReviewData.AssignedTaskSourceType = primaryAssignTaskModel.AssignedTaskSourceType.Name;

            //await _dbContext.SaveChangesAsync();

            //new DbAssessmentAssignTask() { }

            for (int i = 1; i < AssignTaskModel.Count; i++)
            {
                //TODO: Log
                //TODO: Must validate incoming models
                var docRetrievalEvent = new StartWorkflowInstanceEvent
                {
                    CorrelationId = correlationId,
                    WorkflowType = WorkflowType.DbAssessment,
                    ParentProcessId = processId
                };
                //await _eventServiceApiClient.PostEvent(nameof(StartWorkflowInstanceEvent), docRetrievalEvent);
            }

            return RedirectToPage("/Index");
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