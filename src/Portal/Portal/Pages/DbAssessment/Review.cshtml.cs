using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public int ProcessId { get; set; }
        public _TaskInformationModel TaskInformationModel { get; set; }
        public _AssignTaskModel AssignTaskModel { get; set; }
        public _CommentsModel CommentsModel { get; set; }
        public WorkflowDbContext DbContext { get; set; }

        public ReviewModel(WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IHttpContextAccessor httpContextAccessor)
        {
            DbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnGet(int processId)
        {
            ProcessId = processId;

            TaskInformationModel = SetTaskInformationData(processId);
        }

        public IActionResult OnGetRetrieveSourceDocuments(int processId)
        {
            var model = new _SourceDocumentDetailsModel()
            {
                Assessments = DbContext
                    .AssessmentData
                    .Include(a => a.LinkedDocuments)
                    .Where(c => c.ProcessId == processId).ToList(),
                ProcessId = processId
            };

            // Repopulate models...
            OnGet(processId);

            return new PartialViewResult
            {
                ViewName = "_SourceDocumentDetails",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                }
            };
        }

        public IActionResult OnGetRetrieveComments(int processId)
        {
            var model = new _CommentsModel()
            {
                Comments = DbContext.Comment.Where(c => c.ProcessId == processId).ToList(),
                ProcessId = processId
            };

            // Repopulate models...
            OnGet(processId);

            return new PartialViewResult
            {
                ViewName = "_Comments",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                }
            };
        }

        public IActionResult OnGetCommentsPartial(string comment, int processId)
        {
            // TODO: Test with Azure
            // TODO: This will not work in Azure; need alternative; but will work in local dev
            var userId = _httpContextAccessor.HttpContext.User.Identity.Name;

            DbContext.Comment.Add(new Comments
            {
                ProcessId = processId,
                WorkflowInstanceId = DbContext.WorkflowInstance.First(c => c.ProcessId == processId).WorkflowInstanceId,
                Created = DateTime.Now,
                Username = string.IsNullOrEmpty(userId) ? "Unknown" : userId,
                Text = comment
            });

            DbContext.SaveChanges();

            return OnGetRetrieveComments(processId);
        }

        public async Task<IActionResult> OnPostReviewTerminateAsync(string comment, int processId)
        {
            //TODO: Log!

            // TODO: Update WorkflowDB WorkflowInstance table status to Terminated
            // TODO: Add Terminate comment to WorkflowDB Comment table
            // TODO: Terminate process in K2 
            // TODO: Close process in SDRA
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

            var userId = _httpContextAccessor.HttpContext.User.Identity.Name;

            var workflowInstance = await DbContext.WorkflowInstance.Include(wi => wi.AssessmentData)
                .FirstOrDefaultAsync(wi => wi.ProcessId == processId)
                .ConfigureAwait(false);

            if (workflowInstance == null)
            {
                //TODO: Log error!
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the WorkflowInstance table");
            }

            workflowInstance.Status = WorkflowStatus.Terminated.ToString();
            await DbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            //TODO: Log debug!

            DbContext.Comment.Add(new Comments
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstance.WorkflowInstanceId,
                Created = DateTime.Now,
                Username = string.IsNullOrEmpty(userId) ? "Unknown" : userId,
                Text = $"Terminate comment: {comment}"
            });
            await DbContext.SaveChangesAsync()
                .ConfigureAwait(false);

            try
            {
                await _workflowServiceApiClient.TerminateWorkflowInstance(workflowInstance.SerialNumber)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                //TODO: Log error!
            }

            return RedirectToPage("/Index");
        }

        public IActionResult OnGetRetrieveAssignTasks(int processId)
        {
            return new PartialViewResult
            {
                ViewName = "_AssignTask",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = SetAssignTaskData(processId)
                }
            };
        }

        private _TaskInformationModel SetTaskInformationData(int processId)
        {
            if (!System.IO.File.Exists(@"Data\SourceCategories.json")) throw new FileNotFoundException(@"Data\SourceCategories.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\SourceCategories.json");
            var sourceCategories = JsonConvert.DeserializeObject<IEnumerable<SourceCategory>>(jsonString);

            return new _TaskInformationModel
            {
                ProcessId = processId,
                DmEndDate = DateTime.Now,
                DmReceiptDate = DateTime.Now,
                EffectiveReceiptDate = DateTime.Now,
                ExternalEndDate = DateTime.Now,
                OnHold = 4,
                Ion = "2929",
                ActivityCode = "1272",
                SourceCategory = new SourceCategory { SourceCategoryId = 1, Name = "zzzzz" },
                SourceCategories = new SelectList(
                        sourceCategories, "SourceCategoryId", "Name")
            };
        }

        private _AssignTaskModel SetAssignTaskData(int processId)
        {
            return new _AssignTaskModel
            {
                AssignTaskId = ++AssignTaskData.AssignId,    // TODO: AssignTaskData.AssignId: Temporary class for testing; Remove once DB is used to get values
                Ordinal = AssignTaskData.AssignId,
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
            };
        }
    }

    // TODO: Temporary class for testing; Remove once DB is used to get values
    public static class AssignTaskData
    {
        public static int AssignId;
    }
}