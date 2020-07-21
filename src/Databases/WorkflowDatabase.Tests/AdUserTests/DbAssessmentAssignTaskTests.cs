using System;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;
using WorkflowDatabase.Tests.Helpers;

namespace WorkflowDatabase.Tests.AdUserTests
{
    public class DbAssessmentAssignTaskTests
    {
        private WorkflowDbContext _dbContext;
        private AdUser TestUser1 { get; set; }
        private AdUser TestUser2 { get; set; }
        private WorkflowInstance SkeletonWorkflowInstance { get; set; }

        [SetUp]
        public void Setup()
        {
            _dbContext = TestSetupHelper.CreateWorkflowDbContext();
            DatabasesHelpers.ClearWorkflowDbTables(_dbContext);

            TestUser1 = AdUserHelper.CreateTestUser(_dbContext);
            TestUser2 = AdUserHelper.CreateTestUser(_dbContext, 2);

            SkeletonWorkflowInstance = AdUserHelper.CreateSkeletonWorkflowInstance(_dbContext);
        }

        [TearDown]
        public void Teardown()
        {
            _dbContext.Dispose();
        }

        [Test]
        public void Dbassessmentassigntask_table_prevents_unknown_assessoraduser_due_to_FK()
        {
            _dbContext.DbAssessmentAssignTask.Add(new DbAssessmentAssignTask
            {
                ProcessId = SkeletonWorkflowInstance.ProcessId,
                Status = string.Empty,
                AssessorAdUserId = 3
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }
        [Test]
        public void Dbassessmentassigntask_table_prevents_unknown_verifieraduser_due_to_FK()
        {
            _dbContext.DbAssessmentAssignTask.Add(new DbAssessmentAssignTask
            {
                ProcessId = SkeletonWorkflowInstance.ProcessId,
                Status = string.Empty,
                VerifierAdUserId = 3
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

    }
}
