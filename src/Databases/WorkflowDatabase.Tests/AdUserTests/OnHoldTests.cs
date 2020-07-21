using System;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;
using WorkflowDatabase.Tests.Helpers;

namespace WorkflowDatabase.Tests.AdUserTests
{
    public class OnHoldTests
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
        public void OnHold_table_prevents_unknown_OffHoldByAdUser_due_to_FK()
        {
            _dbContext.OnHold.Add(new OnHold
            {
                WorkflowInstanceId = SkeletonWorkflowInstance.WorkflowInstanceId,
                ProcessId = SkeletonWorkflowInstance.ProcessId,
                OnHoldTime = DateTime.Now,
                OnHoldByAdUserId = 1,
                OffHoldByAdUserId = 3
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void OnHold_table_prevents_unknown_OnHoldByAdUser_due_to_FK()
        {
            _dbContext.OnHold.Add(new OnHold
            {
                WorkflowInstanceId = SkeletonWorkflowInstance.WorkflowInstanceId,
                ProcessId = SkeletonWorkflowInstance.ProcessId,
                OnHoldTime = DateTime.Now,
                OnHoldByAdUserId = 3
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }
    }
}

