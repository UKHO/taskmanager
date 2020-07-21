using System;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;
using WorkflowDatabase.Tests.Helpers;

namespace WorkflowDatabase.Tests.AdUserTests
{
    public class TaskNoteTests
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
        public void TaskNote_table_prevents_unknown_LastModifiedByAdUserId_due_to_FK()
        {
            _dbContext.TaskNote.Add(new TaskNote
            {
                WorkflowInstanceId = SkeletonWorkflowInstance.WorkflowInstanceId,
                ProcessId = SkeletonWorkflowInstance.ProcessId,
                Created = DateTime.Now,
                LastModified = DateTime.Now,
                Text = string.Empty,
                CreatedByAdUserId = 1,
                LastModifiedByAdUserId = 3
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void TaskNote_table_prevents_unknown_CreatedByAdUserId_due_to_FK()
        {
            _dbContext.TaskNote.Add(new TaskNote
            {
                WorkflowInstanceId = SkeletonWorkflowInstance.WorkflowInstanceId,
                ProcessId = SkeletonWorkflowInstance.ProcessId,
                Created = DateTime.Now,
                LastModified = DateTime.Now,
                Text = string.Empty,
                CreatedByAdUserId = 3,
                LastModifiedByAdUserId = 1
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }
    }
}

