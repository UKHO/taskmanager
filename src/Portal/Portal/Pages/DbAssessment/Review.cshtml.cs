using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class ReviewModel : PageModel
    {
        public _TaskInformationModel TaskInformationModel { get; set; }
        public _AssignTaskModel AssignTaskModel { get; set; }
        public _CommentsModel CommentsModel { get; set; }

        public void OnGet()
        {
            SetTaskInformationData();
            SetAssignTaskData();
            SetCommentsData();
        }

        private void SetTaskInformationData()
        {
            TaskInformationModel = new _TaskInformationModel
            {
                ProcessId = 98,
                DmEndDate = DateTime.Now,
                DmReceiptDate = DateTime.Now,
                EffectiveReceiptDate = DateTime.Now,
                ExternalEndDate = DateTime.Now,
                OnHold = 4,
                Ion = "2929",
                ActivityCode = "1272",
                SourceCategory = new SourceCategory { SourceCategoryId = 1, Name = "zzzzz" },
                SourceCategories = new SelectList(
                        new List<SourceCategory>
                        {
                            new SourceCategory {SourceCategoryId = 0, Name = "asdas"},
                            new SourceCategory {SourceCategoryId = 1, Name = "zzzzz"}
                        }, "SourceCategoryId", "Name")
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

        private void SetCommentsData()
        {
            CommentsModel = new _CommentsModel
            {
                Comments = new List<Comments>
                {
                    new Comments  {
                        CommentId = 1,
                        ProcessId = 123,
                        WorkflowInstanceId = 1,
                        Text = "This is a sample comment for illustrative purposes.",
                        Created = DateTime.Now,
                        Username = "Ross Sandford"
                    },
                    new Comments  {
                        CommentId = 2,
                        ProcessId = 123,
                        WorkflowInstanceId = 1,
                        Text = "A second comment for your enjoyment.",
                        Created = DateTime.Now.AddDays(-1),
                        Username = "Peter Bates"
                    },
                }
            };
        }
    }
}