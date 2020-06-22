using Common.Helpers;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
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

        [SetUp]
        public void Setup()
        {

            _dbContextOptions = new DbContextOptionsBuilder<DbUpdateWorkflowDbContext>()
                .UseSqlServer(
                    @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=taskmanager-dev-dbupdateworkflowdatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
                .Options;

            _dbContext = new DbUpdateWorkflowDbContext(_dbContextOptions);

            DatabasesHelpers.ClearDbUpdateWorkflowDbTables(_dbContext);


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
                CreatedByUsername = "Rajan",
                Created = DateTime.Now,
                LastModifiedByUsername = "Rajan",
                LastModified = DateTime.Now
            }
            );

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains(
                            "The INSERT statement conflicted with the FOREIGN KEY constraint",
                            StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void Ensure_TaskNote_prevents_duplicate_ProcessID_due_to_UQ()
        {
            var taskInfo = _dbContext.Add<TaskInfo>(new TaskInfo()
            {
                ChartingArea = "Home waters",
                UpdateType = "Update from source",
                AssignedUser = "Hannah Kent",
                TargetDate = DateTime.Now,
                AssignedDate = DateTime.Now,
                Status = "Inprogress",
                Name = "Task Name",
                CurrentStage = "Compile Chart",
                StatusChangeDate = DateTime.Now,
                TaskNote = new TaskNote()
                {
                    Text = "Sample Task Note",
                    CreatedByUsername = "Rajan",
                    Created = DateTime.Now,
                    LastModifiedByUsername = "Rajan",
                    LastModified = DateTime.Now
                }
            });


            _dbContext.SaveChanges();

            var processId = taskInfo.Entity.ProcessId;

            using var newContext = new DbUpdateWorkflowDbContext(_dbContextOptions);

            newContext.Add<TaskNote>(new TaskNote()
            {
                Text = "New Task Note",
                ProcessId = processId,
                CreatedByUsername = "Rajan",
                Created = DateTime.Now,
                LastModifiedByUsername = "Rajan",
                LastModified = DateTime.Now
            });

            var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
            Assert.That(ex.InnerException != null &&
                        ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint",
                            StringComparison.OrdinalIgnoreCase));

        }

        [Test]
        public void Ensure_TaskComment_prevents_Invalid_ProcessId_due_to_FK()
        {
            _dbContext.Add<TaskComment>(new TaskComment()
            {
                ProcessId = 99999,
                Created = DateTime.Now,
                Comment = "Sample comment",
                Username = "Rajan"
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains(
                            "The INSERT statement conflicted with the FOREIGN KEY constraint",
                            StringComparison.OrdinalIgnoreCase));

        }

        [Test]
        public void Ensure_TaskRole_Prevents_Invalid_ProcessId_due_to_FK()
        {
            _dbContext.Add<TaskRole>(new TaskRole()
            {
                ProcessId = 99999,
                Compiler = "Rajan",
                Verifier = "Rajan"
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains(
                            "The INSERT statement conflicted with the FOREIGN KEY constraint",
                            StringComparison.OrdinalIgnoreCase));
        }


        [Test]
        public void Ensure_TaskRole_Prevents_Duplicate_ProcessId_due_to_UQ()
        {
            var taskInfo = _dbContext.Add<TaskInfo>(new TaskInfo()
            {
                ChartingArea = "Home waters",
                UpdateType = "Update from source",
                AssignedUser = "Hannah Kent",
                TargetDate = DateTime.Now,
                AssignedDate = DateTime.Now,
                Status = "Inprogress",
                Name = "Task Name",
                CurrentStage = "Compile Chart",
                StatusChangeDate = DateTime.Now,
                TaskRole = new TaskRole()
                {
                    Compiler = "Hannah Kent",
                    Verifier = "Rajan"

                }
            });

            _dbContext.SaveChanges();

            var processId = taskInfo.Entity.ProcessId;

            using var newContext = new DbUpdateWorkflowDbContext(_dbContextOptions);
            newContext.Add<TaskRole>(new TaskRole()
            {
                ProcessId = processId,
                Compiler = "Hannah Kent",
                Verifier = "Rajan"
            });


            var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
            Assert.That(ex.InnerException != null &&
                        ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint",
                            StringComparison.OrdinalIgnoreCase));
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
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains(
                            "The INSERT statement conflicted with the FOREIGN KEY constraint",
                            StringComparison.OrdinalIgnoreCase));

        }


        [Test]
        public void Ensure_TaskStage_Prevents_Invalid_TaskStageType_due_to_FK()
        {
            _dbContext.TaskStageType.RemoveRange(_dbContext.TaskStageType);

            var taskInfo = _dbContext.Add<TaskInfo>(new TaskInfo()
            {
                AssignedUser = "Hannah Kent",
                AssignedDate = DateTime.Now,
                Status = "Inprogress"
            });

            _dbContext.SaveChanges();

            var processId = taskInfo.Entity.ProcessId;

            _dbContext.Add<TaskStage>(new TaskStage()
            {
                ProcessId = processId,
                Status = "InProgress",
                TaskStageTypeId = 1,
                DateExpected = null,
                DateCompleted = null

            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains(
                            "The INSERT statement conflicted with the FOREIGN KEY constraint",
                            StringComparison.OrdinalIgnoreCase));



        }

        [Test]
        public void Ensure_TaskStage_Prevents_Duplicate_TaskStageType_due_to_UQ()
        {

            _dbContext.TaskStageType.RemoveRange(_dbContext.TaskStageType);

            //Create a single stage type for testing
            _dbContext.Add<TaskStageType>(new TaskStageType()
            { Name = "Specification", TaskStageTypeId = 1, AllowRework = false, SequenceNumber = 1 });

            var taskInfo = _dbContext.Add<TaskInfo>(new TaskInfo()
            {
                ChartingArea = "Home waters",
                UpdateType = "Update from source",
                AssignedUser = "Hannah Kent",
                TargetDate = DateTime.Now,
                AssignedDate = DateTime.Now,
                Status = "Inprogress",
                Name = "Task Name",
                CurrentStage = "Compile Chart",
                StatusChangeDate = DateTime.Now,
                TaskStage = new List<TaskStage>()
                {
                    new TaskStage()
                        {Status = "InProgress", TaskStageTypeId = 1, DateCompleted = null, DateExpected = null}
                }

            });

            _dbContext.SaveChanges();

            var processId = taskInfo.Entity.ProcessId;

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
            Assert.That(ex.InnerException != null &&
                        ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint ",
                            StringComparison.OrdinalIgnoreCase));
        }


        [Test]
        public void Ensure_TaskStageComment_Prevent_Invalid_ProcessId_due_to_FK()
        {
            _dbContext.TaskStageType.RemoveRange(_dbContext.TaskStageType);

            //Create a single stage type for testing
            _dbContext.Add<TaskStageType>(new TaskStageType()
            { Name = "Compile Chart", TaskStageTypeId = 1, AllowRework = false, SequenceNumber = 1 });

            var taskInfo = _dbContext.Add<TaskInfo>(new TaskInfo()
            {

                ChartingArea = "Home waters",
                UpdateType = "Update from source",
                AssignedUser = "Hannah Kent",
                TargetDate = DateTime.Now,
                AssignedDate = DateTime.Now,
                Status = "Inprogress",
                Name = "Task Name",
                CurrentStage = "Compile Chart",
                StatusChangeDate = DateTime.Now,
                TaskStage = new List<TaskStage>()
                {
                    new TaskStage()
                        {Status = "InProgress", TaskStageTypeId = 1, DateCompleted = null, DateExpected = null}
                }

            });

            _dbContext.SaveChanges();

            var processId = taskInfo.Entity.ProcessId;
            var stageId = taskInfo.Entity.TaskStage[0].TaskStageId;


            //Add a comment with valid TaskStageId, but invalid ProcessID
            _dbContext.Add<TaskStageComment>(new TaskStageComment()
            {
                Comment = "Sample Task Stage Comment",
                Created = DateTime.Now,
                Username = "Rajan",
                ProcessId = 9999,
                TaskStageId = stageId
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains(
                            "The INSERT statement conflicted with the FOREIGN KEY constraint",
                            StringComparison.OrdinalIgnoreCase));

        }


        [Test]
        public void Ensure_TaskStageComment_Prevent_Invalid_TaskStageId_due_to_FK()
        {
            _dbContext.TaskStageType.RemoveRange(_dbContext.TaskStageType);
            _dbContext.TaskStage.RemoveRange(_dbContext.TaskStage);

            //Create a single stage type for testing
            _dbContext.Add<TaskStageType>(new TaskStageType()
            { Name = "Compile Chart", TaskStageTypeId = 1, AllowRework = false, SequenceNumber = 1 });

            var taskInfo = _dbContext.Add<TaskInfo>(new TaskInfo()
            {

                ChartingArea = "Home waters",
                UpdateType = "Update from source",
                AssignedUser = "Hannah Kent",
                TargetDate = DateTime.Now,
                AssignedDate = DateTime.Now,
                Status = "Inprogress",
                Name = "Task Name",
                CurrentStage = "Compile Chart",
                StatusChangeDate = DateTime.Now,
                TaskStage = new List<TaskStage>()
                {
                    new TaskStage()
                        {Status = "InProgress", TaskStageTypeId = 1, DateCompleted = null, DateExpected = null}
                }

            });

            _dbContext.SaveChanges();

            var processId = taskInfo.Entity.ProcessId;
            var stageId = taskInfo.Entity.TaskStage[0].TaskStageId;

            //Add a comment with invalid TaskStageId,  a valid ProcessID
            _dbContext.Add<TaskStageComment>(new TaskStageComment()
            {
                Comment = "Sample Task Stage Comment",
                Created = DateTime.Now,
                Username = "Rajan",
                ProcessId = processId,
                TaskStageId = 9999
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException != null && ex.InnerException.Message.Contains(
                            "The INSERT statement conflicted with the FOREIGN KEY constraint",
                            StringComparison.OrdinalIgnoreCase));


        }
    }
}

