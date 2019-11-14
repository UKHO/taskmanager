using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Enums;
using Common.Messages.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class ReviewModel : PageModel
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;

        public int ProcessId { get; set; }
        public bool IsOnHold { get; set; }

        [BindProperty]
        public List<_AssignTaskModel> AssignTaskModel { get; set; }
        public _CommentsModel CommentsModel { get; set; }
        public WorkflowDbContext DbContext { get; set; }

        public ReviewModel(WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            IHttpContextAccessor httpContextAccessor)
        {
            DbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;
            AssignTaskModel = SetAssignTaskDummyData(processId);
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnGetRetrieveComments(int processId)
        {
            var model = new _CommentsModel()
            {
                Comments = DbContext.Comment.Where(c => c.ProcessId == processId).ToList(),
                ProcessId = processId
            };

            // Repopulate models...
            await OnGet(processId);

            return new PartialViewResult
            {
                ViewName = "_Comments",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                }
            };
        }

        public async Task<IActionResult> OnGetCommentsPartialAsync(string comment, int processId)
        {
            // TODO: Test with Azure
            // TODO: This will not work in Azure; need alternative; but will work in local dev

            var workflowInstance = DbContext.WorkflowInstance.First(c => c.ProcessId == processId).WorkflowInstanceId;

            AddComment(comment, processId, workflowInstance);

            return await OnGetRetrieveComments(processId);
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


            var workflowInstance = UpdateWorkflowInstanceAsTerminated(processId);
            AddComment($"Terminate comment: {comment}", processId, workflowInstance.WorkflowInstanceId);
            await UpdateK2WorkflowAsTerminated(workflowInstance);
            await UpdateSdraAssessmentAsCompleted(comment, workflowInstance);

            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId)
        {
            // Work out how many additional Assign Task partials we have, and send a StartWorkflowInstanceEvent for each one
            //TODO: Log

            var correlationId = DbContext.PrimaryDocumentStatus.First(d => d.ProcessId == processId).CorrelationId.Value;

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
                await _eventServiceApiClient.PostEvent(nameof(StartWorkflowInstanceEvent), docRetrievalEvent);
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

        private async Task AddComment(string comment, int processId, int workflowInstanceId)
        {
            var userId = _httpContextAccessor.HttpContext.User.Identity.Name;

            DbContext.Comment.Add(new Comments
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                Created = DateTime.Now,
                Username = string.IsNullOrEmpty(userId) ? "Unknown" : userId,
                Text = comment
            });

            await DbContext.SaveChangesAsync();
        }

        private WorkflowInstance UpdateWorkflowInstanceAsTerminated(int processId)
        {
            var workflowInstance = DbContext.WorkflowInstance
                .Include(wi => wi.AssessmentData)
                .FirstOrDefault(wi => wi.ProcessId == processId);

            if (workflowInstance == null)
            {
                //TODO: Log error!
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the WorkflowInstance table");
            }

            workflowInstance.Status = WorkflowStatus.Terminated.ToString();
            DbContext.SaveChanges();

            return workflowInstance;
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await DbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }

        private List<_AssignTaskModel> SetAssignTaskDummyData(int processId)
        {
            return new List<_AssignTaskModel>{new _AssignTaskModel
            {
                AssignTaskId = 1,    // TODO: AssignTaskData.AssignId: Temporary class for testing; Remove once DB is used to get values
                Ordinal = 1,
                ProcessId = processId,
                Assessor = new Assessor { AssessorId = 1, Name = "Peter Bates" },
                Assessors = new SelectList(
                    new List<Assessor>
                    {
                        new Assessor {AssessorId = 0, Name = "Brian Stenson"},
                        new Assessor {AssessorId = 1, Name = "Peter Bates"}
                    }, "AssessorId", "Name"),
                SourceType = new SourceType { SourceTypeId = 0, Name = "Simple" },
                SourceTypes = new SelectList(
                    new List<SourceType>
                    {
                        new SourceType{SourceTypeId = 0, Name = "Simple"},
                        new SourceType{SourceTypeId = 1, Name = "LTA (Product only)"},
                        new SourceType{SourceTypeId = 2, Name = "LTA"}
                    }, "SourceTypeId", "Name"),
                Verifier = new Verifier { VerifierId = 1, Name = "Matt Stoodley" },
                Verifiers = new SelectList(
                    new List<Verifier>
                    {
                        new Verifier{VerifierId = 0, Name = "Brian Stenson"},
                        new Verifier{VerifierId = 1, Name = "Matt Stoodley"},
                        new Verifier{VerifierId = 2, Name = "Peter Bates"}
                    }, "VerifierId", "Name")
            }};
        }
    }
}