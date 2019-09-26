using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class ReviewModel : PageModel
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public int ProcessId { get; set; }
        public _TaskInformationModel TaskInformationModel { get; set; }
        public _AssignTaskModel AssignTaskModel { get; set; }
        public _CommentsModel CommentsModel { get; set; }
        public WorkflowDbContext DbContext { get; set; }

        public ReviewModel(WorkflowDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            DbContext = dbContext;
        }

        public void OnGet(int processId)
        {
            ProcessId = processId;

            SetTaskInformationData(processId);
            SetAssignTaskData();
            RetrieveComments(processId);
        }

        public async Task<IActionResult> OnPostNewCommentAsync(string comment, int processId)
        {
            // TODO: Repopulate models in a different way
            OnGet(processId);

            //TODO: Find a more robust way to get the username, this is just the result of Googling, currently
            string username = string.Empty;

            try
            {
                username = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            catch (NullReferenceException ex)
            {
                // log "Couldn't get user name..."
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            DbContext.Comment.Add(new Comments
            {
                ProcessId = TaskInformationModel.ProcessId,
                WorkflowInstanceId = DbContext.WorkflowInstance.First(c => c.ProcessId == processId).WorkflowInstanceId,
                Created = DateTime.Now,
                Username = username == string.Empty ? "TBC" : username, //TODO: Username stuff again
                Text = comment
            });

            DbContext.SaveChanges();

            RetrieveComments(processId);

            return new PartialViewResult();
        }

        private void SetTaskInformationData(int processId)
        {
            if (!System.IO.File.Exists(@"Data\SourceCategories.json")) throw new FileNotFoundException(@"Data\SourceCategories.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\SourceCategories.json");
            var sourceCategories = JsonConvert.DeserializeObject<IEnumerable<SourceCategory>>(jsonString);

            TaskInformationModel = new _TaskInformationModel
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

        private void SetAssignTaskData()
        {
            AssignTaskModel = new _AssignTaskModel
            {
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

        private void RetrieveComments(int processId)
        {
            CommentsModel = new _CommentsModel
            {
                ProcessId = processId,
                Comments = DbContext.Comment.Where(c => c.ProcessId == processId).ToList()
            };
        }
    }
}