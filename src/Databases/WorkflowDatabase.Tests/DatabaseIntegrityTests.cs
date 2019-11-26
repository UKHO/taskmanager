using System;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.Tests
{
    public class DatabaseIntegrityTests
    {
        private WorkflowDbContext _dbContext;
        private DbContextOptions<WorkflowDbContext> _dbContextOptions;

        [SetUp]
        public void Setup()
        {
            _dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(
                    @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=taskmanager-dev-workflowdatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
                .Options;

            _dbContext = new WorkflowDbContext(_dbContextOptions);

            DatabasesHelpers.ClearWorkflowDbTables(_dbContext);
        }

        [TearDown]
        public void Teardown()
        {
            _dbContext.Dispose();
        }


        [Test]
        public void Ensure_dbassessmentreviewdata_table_prevents_duplicate_workflowinstanceid_due_to_FK()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                WorkflowType = "DbAssessment",
                ActivityName = "Review"
            });

            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                Assessor = "Me",
                WorkflowInstanceId = 1,
                Verifier = "Someone",
                ActivityCode = "Act666",
                TaskComplexity = "Simples"
            });

            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                Assessor = "Me",
                WorkflowInstanceId = 1,
                Verifier = "You",
                ActivityCode = "Act111",
                TaskComplexity = "Simples"
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void Ensure_workflowinstance_table_prevents_duplicate_processid_due_to_UQ()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                WorkflowType = WorkflowConstants.WorkflowType,
                ActivityName = WorkflowConstants.ActivityName,
                Status = WorkflowStatus.Started.ToString(),
                StartedAt = DateTime.Now
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.WorkflowInstance.Add(new WorkflowInstance()
                {
                    ProcessId = 1,
                    SerialNumber = "2_sn",
                    ParentProcessId = null,
                    WorkflowType = WorkflowConstants.WorkflowType,
                    ActivityName = WorkflowConstants.ActivityName,
                    Status = WorkflowStatus.Started.ToString(),
                    StartedAt = DateTime.Now
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void Ensure_primarydocumentstatus_table_prevents_duplicate_processId_due_to_UQ()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                WorkflowType = "DbAssessment",
                ActivityName = "Review",
                StartedAt = DateTime.Now,
                Status = WorkflowStatus.Started.ToString()
            });

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = 1,
                SdocId = 12345,
                ContentServiceId = Guid.NewGuid(),
                StartedAt = DateTime.Now,
                Status = "Started"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
                {
                    ProcessId = 1,
                    SdocId = 12346,
                    ContentServiceId = Guid.NewGuid(),
                    StartedAt = DateTime.Now,
                    Status = "Started"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void Ensure_primarydocumentstatus_table_prevents_duplicate_processid_due_to_UQ()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "2_sn",
                ParentProcessId = null,
                WorkflowType = WorkflowConstants.WorkflowType,
                ActivityName = WorkflowConstants.ActivityName,
                Status = WorkflowStatus.Started.ToString(),
                StartedAt = DateTime.Now
            });
            _dbContext.SaveChanges();

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = 1,
                SdocId = 12345,
                ContentServiceId = Guid.NewGuid(),
                StartedAt = DateTime.Now,
                Status = "Started"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
                {
                    ProcessId = 1,
                    SdocId = 12345,
                    ContentServiceId = Guid.NewGuid(),
                    StartedAt = DateTime.Now,
                    Status = "Started"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void Ensure_Comments_table_prevents_insert_for_no_WorkflowInstance()
        { 
            _dbContext.Comment.AddAsync(new WorkflowDatabase.EF.Models.Comments()
            {
                Created = DateTime.Now,
                ProcessId = 0,
                Text = "This is a comment",
                Username = "Me",
                WorkflowInstanceId = 555
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void Ensure_LinkedDocument_table_prevents_insert_for_no_ProcessId()
        {
            _dbContext.LinkedDocument.AddAsync(new LinkedDocuments()
            {
                PrimarySdocId = 1234,
                LinkType = "Forward",
                RsdraNumber = "x345",
                LinkedSdocId = 5678,
                SourceDocumentName = "terstingf",
                Created = DateTime.Now,
                Status = LinkedDocumentRetrievalStatus.Started.ToString()
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void Ensure_hpdusage_table_prevents_duplicate_name_due_to_UQ()
        {
            _dbContext.HpdUsage.Add(new HpdUsage
            {
                Name = "Offshore Energy"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.HpdUsage.Add(new HpdUsage
                {
                    Name = "Offshore Energy"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}

