using System;
using System.Threading.Tasks;
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
                ActivityName = "Review"
            });

            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                Assessor = "Me",
                WorkflowInstanceId = 1,
                Verifier = "Someone",
                ActivityCode = "Act666"
            });

            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                Assessor = "Me",
                WorkflowInstanceId = 1,
                Verifier = "You",
                ActivityCode = "Act111"
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void Ensure_dbassessmentassessdata_table_prevents_duplicate_workflowinstanceid_due_to_FK()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                ActivityName = "Assess"
            });

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                Assessor = "Me",
                WorkflowInstanceId = 1,
                Verifier = "Someone",
                ActivityCode = "Act666",
                TaskType = "Simples"
            });

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                Assessor = "Me",
                WorkflowInstanceId = 1,
                Verifier = "You",
                ActivityCode = "Act111",
                TaskType = "Simples"
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
                ActivityName = WorkflowStage.Review.ToString(),
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
                    ActivityName = WorkflowStage.Review.ToString(),
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
                ActivityName = WorkflowStage.Review.ToString(),
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
            _dbContext.Comment.AddAsync(new WorkflowDatabase.EF.Models.Comment()
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
            _dbContext.LinkedDocument.AddAsync(new LinkedDocument()
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
                HpdUsageId = 1,
                Name = "Offshore Energy"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.HpdUsage.Add(new HpdUsage
                {
                    HpdUsageId = 2,
                    Name = "Offshore Energy"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void Ensure_hpduser_table_prevents_duplicate_adusername_due_to_UQ()
        {
            _dbContext.HpdUser.Add(new HpdUser
            {
                AdUsername = "TMAccount1",
                HpdUsername = "Person1"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.HpdUser.Add(new HpdUser
                {
                    AdUsername = "TMAccount1",
                    HpdUsername = "Person2"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void Ensure_hpduser_table_prevents_duplicate_hpdusername_due_to_UQ()
        {
            _dbContext.HpdUser.Add(new HpdUser
            {
                AdUsername = "TMAccount1",
                HpdUsername = "Person1"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.HpdUser.Add(new HpdUser
                {
                    AdUsername = "TMAccount2",
                    HpdUsername = "Person1"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void Ensure_productAction_table_allows_duplicate_productActiontypeId_for_the_same_processId()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 123,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                ActivityName = "Review",
                StartedAt = DateTime.Today,
                Status = "Started"

            });

            _dbContext.ProductActionType.Add(new ProductActionType()
            {
                ProductActionTypeId = 1,
                Name = "CPTS/IA"
            });

            _dbContext.SaveChanges();

            _dbContext.ProductAction.Add(new ProductAction()
            {
                ProcessId = 123,
                ImpactedProduct = "GB1234",
                ProductActionTypeId = 1,
                Verified = false
            });

            _dbContext.ProductAction.Add(new ProductAction()
            {
                ProcessId = 123,
                ImpactedProduct = "GB5678",
                ProductActionTypeId = 1,
                Verified = false
            });

            Assert.DoesNotThrow(() => _dbContext.SaveChanges());
        }

        [Test]
        public void Ensure_productAction_table_allows_duplicate_productActiontypeId_for_different_processId()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 123,
                PrimarySdocId = 1111,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                ActivityName = "Review",
                StartedAt = DateTime.Today,
                Status = "Started"

            });

            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 124,
                PrimarySdocId = 2222,
                SerialNumber = "2_sn",
                ParentProcessId = null,
                ActivityName = "Review",
                StartedAt = DateTime.Today,
                Status = "Started"
            });


            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 125,
                PrimarySdocId = 3333,
                SerialNumber = "3_sn",
                ParentProcessId = null,
                ActivityName = "Review",
                StartedAt = DateTime.Today,
                Status = "Started"
            });

            _dbContext.ProductActionType.Add(new ProductActionType()
            {
                ProductActionTypeId = 1,
                Name = "CPTS/IA"
            });

            _dbContext.SaveChanges();

            _dbContext.ProductAction.Add(new ProductAction()
            {
                ProcessId = 123,
                ImpactedProduct = "GB1234",
                ProductActionTypeId = 1,
                Verified = false
            });

            _dbContext.ProductAction.Add(new ProductAction()
            {
                ProcessId = 124,
                ImpactedProduct = "GB5678",
                ProductActionTypeId = 1,
                Verified = false
            });

            _dbContext.ProductAction.Add(new ProductAction()
            {
                ProcessId = 125,
                ImpactedProduct = "GB1111",
                ProductActionTypeId = 1,
                Verified = false
            });

            var newProductActionCount = _dbContext.SaveChanges();

            Assert.AreEqual(3, newProductActionCount);
        }

        [Test]
        public void Ensure_productactiontype_table_prevents_duplicate_name_due_to_UQ()
        {
            _dbContext.ProductActionType.Add(new ProductActionType()
            {
                ProductActionTypeId = 1,
                Name = "CPTS/IA"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.ProductActionType.Add(new ProductActionType
                {
                    ProductActionTypeId = 2,
                    Name = "CPTS/IA"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }


        [Test]
        public void Ensure_CachedHpdWorkspace_table_prevents_duplicate_name_due_to_UQ()
        {
            _dbContext.CachedHpdWorkspace.Add(new CachedHpdWorkspace()
            {
                Name = "testing workspace"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.CachedHpdWorkspace.Add(new CachedHpdWorkspace()
                {
                    Name = "testing workspace"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase));
            }
        }



        [Test]
        public void Ensure_assignedTaskType_table_prevents_duplicate_name_due_to_UQ()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType()
            {
                AssignedTaskTypeId = 1,
                Name = "Offshore Greg Energy"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.AssignedTaskType.Add(new AssignedTaskType()
                {
                    AssignedTaskTypeId = 2,
                    Name = "Offshore Greg Energy"
                });

                var ex = Assert.Throws<DbUpdateException>(() => newContext.SaveChanges());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public void Ensure_DbAssessmentAssignTask_table_prevents_insert_for_no_ProcessId()
        {
            _dbContext.DbAssessmentAssignTask.AddAsync(new DbAssessmentAssignTask
            {
                Assessor = "Greg",
                TaskType = "Type 1",
                Notes = "A note",
                Verifier = "Ross",
                WorkspaceAffected = "Workspace 1"
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task Ensure_CarisProjectDetails_table_prevents_duplicate_ProcessId_due_to_UQ()
        {
            await _dbContext.WorkflowInstance.AddAsync(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                ActivityName = "Assess",
                StartedAt = DateTime.Now,
                Status = "Started"
            });

            await _dbContext.SaveChangesAsync();

            await _dbContext.CarisProjectDetails.AddAsync(new CarisProjectDetails
            {
                ProcessId = 1,
                ProjectName = "TestProject",
                CreatedBy = "TestUser",
                Created = DateTime.Now,
                ProjectId = 1
            });

            await _dbContext.SaveChangesAsync();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                await newContext.CarisProjectDetails.AddAsync(new CarisProjectDetails
                {
                    ProcessId = 1,
                    ProjectName = "TestProject2",
                    CreatedBy = "TestUser2",
                    Created = DateTime.Now.AddDays(1),
                    ProjectId = 2
                });

                var ex = Assert.ThrowsAsync<DbUpdateException>(() => newContext.SaveChangesAsync());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }

            
        }
    }
}

