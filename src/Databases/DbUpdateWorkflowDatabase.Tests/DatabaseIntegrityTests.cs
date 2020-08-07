using Common.Helpers;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using DbUpdateWorkflowDatabase.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;


namespace DbUpdateWorkflowDatabase.Tests
{
    public class DatabaseIntegrityTests
    {
        private DbUpdateWorkflowDbContext _dbContext;
        private DbContextOptions<DbUpdateWorkflowDbContext> _dbContextOptions;

        public AdUser TestUser { get; set; }
        public AdUser TestUser2 { get; set; }

        [SetUp]
        public void Setup()
        {

            _dbContextOptions = new DbContextOptionsBuilder<DbUpdateWorkflowDbContext>()
                .UseSqlServer(
                    @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=taskmanager-dev-dbupdateworkflowdatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
                .Options;

            _dbContext = new DbUpdateWorkflowDbContext(_dbContextOptions);

            DatabasesHelpers.ClearDbUpdateWorkflowDbTables(_dbContext);

            TestUser = AdUserHelper.CreateTestUser(_dbContext);
            TestUser2 = AdUserHelper.CreateTestUser(_dbContext, 2);

        }

        [TearDown]
        public void Teardown()
        {
            _dbContext.Dispose();
        }

        [Test]
        public void Ensure_TaskNote_prevents_Invalid_ProcessID__due_to_FK()
        {
            _dbContext.Add<TaskNote>(new TaskNote()
            {
                ProcessId = 99999,
                Text = "Task Note example",
                CreatedBy = TestUser,
                Created = DateTime.Now,
                LastModifiedBy = TestUser2,
                LastModified = DateTime.Now
            }
            );

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void Ensure_TaskNote_prevents_duplicate_ProcessID_due_to_UQ()
        {
            var taskInfo = AdUserHelper.CreateSkeletonTaskInfoInstance(_dbContext, TestUser);

            taskInfo.TaskNote =

                new TaskNote()
                {
                    Text = "Sample Task Note",
                    CreatedBy = TestUser,
                    Created = DateTime.Now,
                    LastModifiedBy = TestUser2,
                    LastModified = DateTime.Now
                };

            _dbContext.SaveChanges();

            var processId = taskInfo.ProcessId;

            using var newContext = new DbUpdateWorkflowDbContext(_dbContextOptions);

            var testuser3 = AdUserHelper.CreateTestUser(newContext, 3);

            newContext.Add<TaskNote>(new TaskNote()
            {
                Text = "New Task Note",
                ProcessId = processId,
                CreatedBy = testuser3,
                Created = DateTime.Now,
                LastModifiedBy = testuser3,
                LastModified = DateTime.Now
            });


            var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());

            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));

        }

        [Test]
        public void Ensure_TaskComment_prevents_Invalid_ProcessId_due_to_FK()
        {
            _dbContext.Add<TaskComment>(new TaskComment()
            {
                ProcessId = 99999,
                Created = DateTime.Now,
                Comment = "Sample comment",
                AdUser = TestUser
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));

        }

        [Test]
        public void Ensure_TaskRole_Prevents_Invalid_ProcessId_due_to_FK()
        {
            _dbContext.Add<TaskRole>(new TaskRole()
            {
                ProcessId = 99999,
                Compiler = TestUser,
                Verifier = TestUser2
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }


        [Test]
        public void Ensure_TaskRole_Prevents_Duplicate_ProcessId_due_to_UQ()
        {

            var taskInfo = AdUserHelper.CreateSkeletonTaskInfoInstance(_dbContext, TestUser);

            taskInfo.TaskRole = new TaskRole()
            {
                Compiler = TestUser,
                Verifier = TestUser2
            };


            _dbContext.SaveChanges();

            var processId = taskInfo.ProcessId;

            using var newContext = new DbUpdateWorkflowDbContext(_dbContextOptions);

            var testuser4 = AdUserHelper.CreateTestUser(newContext, 4);

            newContext.Add<TaskRole>(new TaskRole()
            {
                ProcessId = processId,
                Compiler = testuser4,
                Verifier = testuser4
            });


            var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void Ensure_TaskStage_Prevents_Invalid_ProcessId_due_to_FK()
        {
            _dbContext.Add<TaskStage>(new TaskStage()
            {
                ProcessId = 99999,
                TaskStageTypeId = 1,
                Status = "In Progress",
                DateExpected = null,
                DateCompleted = null
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));

        }


        [Test]
        public void Ensure_TaskStage_Prevents_Invalid_TaskStageType_due_to_FK()
        {
            _dbContext.TaskStageType.RemoveRange(_dbContext.TaskStageType);

            var taskInfo = AdUserHelper.CreateSkeletonTaskInfoInstance(_dbContext, TestUser);

            _dbContext.SaveChanges();

            var processId = taskInfo.ProcessId;

            _dbContext.Add<TaskStage>(new TaskStage()
            {
                ProcessId = processId,
                Status = "InProgress",
                TaskStageTypeId = 1,
                DateExpected = null,
                DateCompleted = null

            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));



        }

        [Test]
        public void Ensure_TaskStage_Prevents_Duplicate_TaskStageType_due_to_UQ()
        {

            _dbContext.TaskStageType.RemoveRange(_dbContext.TaskStageType);

            //Create a single stage type for testing
            _dbContext.Add<TaskStageType>(new TaskStageType()
            { Name = "Specification", TaskStageTypeId = 1, AllowRework = false, SequenceNumber = 1 });


            var taskInfo = AdUserHelper.CreateSkeletonTaskInfoInstance(_dbContext, TestUser);

            taskInfo.TaskStage = new List<TaskStage>()
            {
                new TaskStage()
                    {Status = "InProgress", TaskStageTypeId = 1, DateCompleted = null, DateExpected = null}
            };

            _dbContext.SaveChanges();

            var processId = taskInfo.ProcessId;

            using var newContext = new DbUpdateWorkflowDbContext(_dbContextOptions);
            newContext.Add<TaskStage>(new TaskStage()
            {
                ProcessId = processId,
                Status = "InProgress",
                TaskStageTypeId = 1,
                DateExpected = null,
                DateCompleted = null
            });

            var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint ", StringComparison.OrdinalIgnoreCase));
        }


        [Test]
        public void Ensure_TaskStageComment_Prevent_Invalid_ProcessId_due_to_FK()
        {
            _dbContext.TaskStageType.RemoveRange(_dbContext.TaskStageType);

            //Create a single stage type for testing
            _dbContext.Add<TaskStageType>(new TaskStageType()
            { Name = "Compile Chart", TaskStageTypeId = 1, AllowRework = false, SequenceNumber = 1 });

            var taskInfo = AdUserHelper.CreateSkeletonTaskInfoInstance(_dbContext, TestUser);

            taskInfo.TaskStage = new List<TaskStage>()
            {
                new TaskStage()
                    {Status = "InProgress", TaskStageTypeId = 1, DateCompleted = null, DateExpected = null}
            };

            _dbContext.SaveChanges();

            var processId = taskInfo.ProcessId;
            var stageId = taskInfo.TaskStage[0].TaskStageId;

            //Add a comment with valid TaskStageId, but invalid ProcessID
            _dbContext.Add<TaskStageComment>(new TaskStageComment()
            {
                Comment = "Sample Task Stage Comment",
                Created = DateTime.Now,
                AdUser = TestUser,
                ProcessId = 9999,
                TaskStageId = stageId
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));

        }

        [Test]
        public void Ensure_TaskStageComment_Prevent_Invalid_TaskStageId_due_to_FK()
        {
            _dbContext.TaskStageType.RemoveRange(_dbContext.TaskStageType);
            _dbContext.TaskStage.RemoveRange(_dbContext.TaskStage);

            //Create a single stage type for testing
            _dbContext.Add<TaskStageType>(new TaskStageType()
            { Name = "Specification", TaskStageTypeId = 1, AllowRework = false, SequenceNumber = 1 });

            var taskInfo = AdUserHelper.CreateSkeletonTaskInfoInstance(_dbContext, TestUser);

            taskInfo.TaskStage
                = new List<TaskStage>()
                {
                    new TaskStage()
                        {Status = "InProgress", TaskStageTypeId = 1, DateCompleted = null, DateExpected = null}
                };



            _dbContext.SaveChanges();

            var processId = taskInfo.ProcessId;
            var stageId = taskInfo.TaskStage[0].TaskStageId;

            //Add a comment with invalid TaskStageId,  a valid ProcessID
            _dbContext.Add<TaskStageComment>(new TaskStageComment()
            {
                Comment = "Sample Task Stage Comment",
                Created = DateTime.Now,
                AdUser = TestUser,
                ProcessId = processId,
                TaskStageId = 9999
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));


        }
    }
}

