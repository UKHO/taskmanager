using System;
using System.Linq;
using Common.Helpers;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.TestAutomation.Framework
{
    public class TestData : TestWorkflowDatabaseSeeder
    {
        private TestData(WorkflowDbContext context) : base(context)
        {
        }

        public TestData AddUser(string userToAdd)
        {
            var rand = new Random();

            _context.AdUser.Add(new AdUser
            {
                ActiveDirectorySid = rand.Next(1000, 100000).ToString(),
                DisplayName = userToAdd,
                LastCheckedDate = DateTime.Today.AddDays(1)
            });

            _context.HpdUser.Add(new HpdUser
                {AdUsername = userToAdd, HpdUsername = userToAdd.Replace(" ", "") + "-Caris"});

            return this;
        }

        public TestData ReassignReviewsToUser(string user)
        {
            var inProgressWorkflows =
                _context.WorkflowInstance.Where(wi => wi.Status == WorkflowStatus.Started.ToString());

            var workflowAtAssessId = inProgressWorkflows.First(w => w.ActivityName == WorkflowStage.Assess.ToString())
                .WorkflowInstanceId;
            var assess = _context.DbAssessmentAssessData.First(x => x.WorkflowInstanceId == workflowAtAssessId);
            assess.Reviewer = user;


            var workflowAtReviewId = inProgressWorkflows.First(w => w.ActivityName == WorkflowStage.Review.ToString())
                .WorkflowInstanceId;
            var review = _context.DbAssessmentReviewData.First(x => x.WorkflowInstanceId == workflowAtReviewId);
            review.Reviewer = user;


            var workflowAtVerifyId = inProgressWorkflows.First(w => w.ActivityName == WorkflowStage.Verify.ToString())
                .WorkflowInstanceId;
            var verify = _context.DbAssessmentVerifyData.First(x => x.WorkflowInstanceId == workflowAtVerifyId);
            verify.Reviewer = user;

            return this;
        }

        public new static TestData UsingDbContext(WorkflowDbContext context)
        {
            return new TestData(context);
        }
    }
}