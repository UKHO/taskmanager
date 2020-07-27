using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.TestAutomation.Framework
{
    public class TestData : TestWorkflowDatabaseSeeder
    {
        private TestData(string workflowDbConnectionString) : base(workflowDbConnectionString)
        {
        }

        // TODO rework temporary concat email domain

        public TestData AddUser(string userToAdd)
        {
            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);

            var newUser = new AdUser
            {
                UserPrincipalName = userToAdd + "@ukho.gov.uk",
                DisplayName = userToAdd,
                LastCheckedDate = DateTime.Today.AddDays(1)
            };

            workflowDbContext.AdUsers.Add(newUser);


            workflowDbContext.HpdUser.Add(new HpdUser
            { AdUser = newUser, HpdUsername = userToAdd.Replace(" ", "") + "-Caris" });

            workflowDbContext.SaveChanges();

            return this;
        }

        public TestData ReassignReviewsToUser(string user)
        {
            using var workflowDbContext = new WorkflowDbContext(_dbContextOptions);

            var adUser = workflowDbContext.AdUsers.Single(u => u.UserPrincipalName == user + "@ukho.gov.uk");

            var inProgressWorkflows =
                workflowDbContext.WorkflowInstance.Where(wi => wi.Status == WorkflowStatus.Started.ToString());

            var workflowAtAssessId = inProgressWorkflows.First(w => w.ActivityName == WorkflowStage.Assess.ToString())
                .WorkflowInstanceId;
            var assess = workflowDbContext.DbAssessmentAssessData.First(x => x.WorkflowInstanceId == workflowAtAssessId);
            assess.Reviewer = adUser;


            var workflowAtReviewId = inProgressWorkflows.First(w => w.ActivityName == WorkflowStage.Review.ToString())
                .WorkflowInstanceId;
            var review = workflowDbContext.DbAssessmentReviewData.First(x => x.WorkflowInstanceId == workflowAtReviewId);
            review.Reviewer = adUser;


            var workflowAtVerifyId = inProgressWorkflows.First(w => w.ActivityName == WorkflowStage.Verify.ToString())
                .WorkflowInstanceId;
            var verify = workflowDbContext.DbAssessmentVerifyData.First(x => x.WorkflowInstanceId == workflowAtVerifyId);
            verify.Reviewer = adUser;

            return this;
        }

        public new static TestData UsingDbConnectionString(string workflowDbConnectionString)
        {
            return new TestData(workflowDbConnectionString);
        }
    }
}