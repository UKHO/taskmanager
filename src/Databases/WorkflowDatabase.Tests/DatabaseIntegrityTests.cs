using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;
using WorkflowDatabase.Tests.Helpers;

namespace WorkflowDatabase.Tests
{
    public class DatabaseIntegrityTests
    {
        private WorkflowDbContext _dbContext;
        private DbContextOptions<WorkflowDbContext> _dbContextOptions;

        public AdUser TestUser { get; set; }
        public AdUser TestUser2 { get; set; }

        [SetUp]
        public void Setup()
        {
            _dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(
                    @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=taskmanager-dev-workflowdatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
                .Options;

            _dbContext = new WorkflowDbContext(_dbContextOptions);
            DatabasesHelpers.ClearWorkflowDbTables(_dbContext);

            TestUser = AdUserHelper.CreateTestUser(_dbContext);
            TestUser2 = AdUserHelper.CreateTestUser(_dbContext, 2);
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
                Assessor = null,
                WorkflowInstanceId = 1,
                Verifier = null,
                ActivityCode = "Act666"
            });

            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                Assessor = null,
                WorkflowInstanceId = 1,
                Verifier = null,
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
                Assessor = null,
                WorkflowInstanceId = 1,
                Verifier = null,
                ActivityCode = "Act666",
                TaskType = "Simples"
            });

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                Assessor = null,
                WorkflowInstanceId = 1,
                Verifier = null,
                ActivityCode = "Act111",
                TaskType = "Simples"
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task Ensure_workflowinstance_table_prevents_duplicate_processid_due_to_UQ()
        {
            await _dbContext.WorkflowInstance.AddAsync(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Started.ToString(),
                StartedAt = DateTime.Now
            });
            await _dbContext.SaveChangesAsync();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                await newContext.WorkflowInstance.AddAsync(new WorkflowInstance()
                {
                    ProcessId = 1,
                    SerialNumber = "2_sn",
                    ParentProcessId = null,
                    ActivityName = WorkflowStage.Review.ToString(),
                    Status = WorkflowStatus.Started.ToString(),
                    StartedAt = DateTime.Now
                });

                var ex = Assert.ThrowsAsync<DbUpdateException>(() => newContext.SaveChangesAsync());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public async Task Ensure_workflowinstance_table_prevents_duplicate_processid_primarySdocId_due_to_composite_UQ()
        {
            await _dbContext.WorkflowInstance.AddAsync(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                PrimarySdocId = 1111,
                ParentProcessId = null,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Started.ToString(),
                StartedAt = DateTime.Now
            });
            await _dbContext.SaveChangesAsync();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                await newContext.WorkflowInstance.AddAsync(new WorkflowInstance()
                {
                    ProcessId = 1,
                    SerialNumber = "2_sn",
                    PrimarySdocId = 1111,
                    ParentProcessId = null,
                    ActivityName = WorkflowStage.Review.ToString(),
                    Status = WorkflowStatus.Started.ToString(),
                    StartedAt = DateTime.Now
                });

                var ex = Assert.ThrowsAsync<DbUpdateException>(() => newContext.SaveChangesAsync());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        public async Task Ensure_workflowinstance_table_allow_non_duplicated_processid_primarySdocId_due_to_composite_UQ()
        {
            await _dbContext.WorkflowInstance.AddAsync(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                PrimarySdocId = 1111,
                ParentProcessId = null,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Started.ToString(),
                StartedAt = DateTime.Now
            });
            await _dbContext.SaveChangesAsync();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                await newContext.WorkflowInstance.AddAsync(new WorkflowInstance()
                {
                    ProcessId = 2,
                    SerialNumber = "3_sn",
                    PrimarySdocId = 1111,
                    ParentProcessId = null,
                    ActivityName = WorkflowStage.Review.ToString(),
                    Status = WorkflowStatus.Started.ToString(),
                    StartedAt = DateTime.Now
                });

                Assert.DoesNotThrowAsync(() => newContext.SaveChangesAsync());
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
            _dbContext.Comments.AddAsync(new WorkflowDatabase.EF.Models.Comment()
            {
                Created = DateTime.Now,
                ProcessId = 0,
                Text = "This is a comment",
                AdUser = _dbContext.AdUsers.Single(u => u.UserPrincipalName == TestUser.UserPrincipalName),
                WorkflowInstanceId = 555
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task Ensure_LinkedDocument_table_prevents_insert_for_no_ProcessId()
        {
            await _dbContext.LinkedDocument.AddAsync(new LinkedDocument()
            {
                PrimarySdocId = 1234,
                LinkType = DocumentLinkType.Forward.ToString(),
                RsdraNumber = "x345",
                LinkedSdocId = 5678,
                SourceDocumentName = "terstingf",
                Created = DateTime.Now,
                Status = SourceDocumentRetrievalStatus.Started.ToString()
            });

            var ex = Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task Ensure_LinkedDocument_table_prevents_insert_for_duplicated_ProcessId_LinkedSdocId_LinkType_due_to_composite_UQ()
        {
            await _dbContext.WorkflowInstance.AddAsync(new WorkflowInstance()
            {
                ProcessId = 12345,
                SerialNumber = "1_sn",
                PrimarySdocId = 1111,
                ParentProcessId = null,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Started.ToString(),
                StartedAt = DateTime.Now
            });

            await _dbContext.LinkedDocument.AddAsync(new LinkedDocument()
            {
                ProcessId = 12345,
                PrimarySdocId = 1111,
                LinkType = DocumentLinkType.Forward.ToString(),
                RsdraNumber = "x345",
                LinkedSdocId = 5678,
                SourceDocumentName = "terstingf",
                Created = DateTime.Now,
                Status = SourceDocumentRetrievalStatus.Started.ToString()
            });

            await _dbContext.SaveChangesAsync();

            await _dbContext.LinkedDocument.AddAsync(new LinkedDocument()
            {
                ProcessId = 12345,
                PrimarySdocId = 1111,
                LinkType = DocumentLinkType.Forward.ToString(),
                RsdraNumber = "x345",
                LinkedSdocId = 5678,
                SourceDocumentName = "terstingf",
                Created = DateTime.Now,
                Status = SourceDocumentRetrievalStatus.Started.ToString()
            });

            var ex = Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
            Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
        }


        [Test]
        public async Task Ensure_LinkedDocument_table_allows_insert_for_non_duplicated_ProcessId_LinkedSdocId_LinkType_due_to_composite_UQ()
        {
            await _dbContext.WorkflowInstance.AddAsync(new WorkflowInstance()
            {
                ProcessId = 12345,
                SerialNumber = "1_sn",
                PrimarySdocId = 1111,
                ParentProcessId = null,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Started.ToString(),
                StartedAt = DateTime.Now
            });

            await _dbContext.LinkedDocument.AddAsync(new LinkedDocument()
            {
                ProcessId = 12345,
                PrimarySdocId = 1111,
                LinkType = DocumentLinkType.Forward.ToString(),
                RsdraNumber = "x345",
                LinkedSdocId = 5678,
                SourceDocumentName = "terstingf",
                Created = DateTime.Now,
                Status = SourceDocumentRetrievalStatus.Started.ToString()
            });

            await _dbContext.SaveChangesAsync();

            await _dbContext.LinkedDocument.AddAsync(new LinkedDocument()
            {
                ProcessId = 12345,
                PrimarySdocId = 1111,
                LinkType = DocumentLinkType.Backward.ToString(),
                RsdraNumber = "x345",
                LinkedSdocId = 5678,
                SourceDocumentName = "terstingf",
                Created = DateTime.Now,
                Status = SourceDocumentRetrievalStatus.Started.ToString()
            });

            Assert.DoesNotThrowAsync(() => _dbContext.SaveChangesAsync());
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
                AdUser = _dbContext.AdUsers.Single(u => u.UserPrincipalName == TestUser.UserPrincipalName),
                HpdUsername = "Person1"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.HpdUser.Add(new HpdUser
                {
                    AdUser = newContext.AdUsers.Single(u => u.UserPrincipalName == TestUser.UserPrincipalName),
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
                AdUser = _dbContext.AdUsers.Single(u => u.UserPrincipalName == TestUser.UserPrincipalName),
                HpdUsername = "Person1"
            });
            _dbContext.SaveChanges();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.HpdUser.Add(new HpdUser
                {
                    AdUser = newContext.AdUsers.Single(u => u.UserPrincipalName == TestUser2.UserPrincipalName),
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
                Assessor = null,
                TaskType = "Type 1",
                Notes = "A note",
                Verifier = null,
                WorkspaceAffected = "Workspace 1",
                Status = AssignTaskStatus.New.ToString()
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("The INSERT statement conflicted with the FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task Ensure_CarisProjectDetails_table_prevents_duplicate_ProcessId_due_to_UQ()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                ActivityName = "Assess",
                StartedAt = DateTime.Now,
                Status = "Started"
            });

            await _dbContext.SaveChangesAsync();

            _dbContext.CarisProjectDetails.Add(new CarisProjectDetails
            {
                ProcessId = 1,
                ProjectName = "TestProject",
                CreatedBy = _dbContext.AdUsers.Single(u => u.UserPrincipalName == TestUser.UserPrincipalName),
                Created = DateTime.Now,
                ProjectId = 1
            });

            await _dbContext.SaveChangesAsync();

            using (var newContext = new WorkflowDbContext(_dbContextOptions))
            {
                newContext.CarisProjectDetails.Add(new CarisProjectDetails
                {
                    ProcessId = 1,
                    ProjectName = "TestProject2",
                    CreatedBy = newContext.AdUsers.Single(u => u.UserPrincipalName == TestUser2.UserPrincipalName),
                    Created = DateTime.Now.AddDays(1),
                    ProjectId = 2
                });

                var ex = Assert.ThrowsAsync<DbUpdateException>(async () => await newContext.SaveChangesAsync());
                Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint", StringComparison.OrdinalIgnoreCase));
            }


        }
    }
}

