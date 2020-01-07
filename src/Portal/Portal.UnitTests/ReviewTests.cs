using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Portal.Auth;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class ReviewTests
    {
        private WorkflowDbContext _dbContext;
        private ReviewModel _reviewModel;
        private int ProcessId { get; set; }
        private IUserIdentityService _fakeUserIdentityService;
        private ILogger<ReviewModel> _fakeLogger;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            ProcessId = 123;

            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                ProcessId = ProcessId
            });

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = ProcessId,
                CorrelationId = Guid.NewGuid()
            });

            _dbContext.SaveChanges();

            _fakeUserIdentityService = A.Fake<IUserIdentityService>();

            _fakeLogger = A.Dummy<ILogger<ReviewModel>>();

            _reviewModel = new ReviewModel(_dbContext, null, null, null, null, _fakeUserIdentityService, _fakeLogger);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_entering_a_primary_sourcetype_that_does_not_exist_results_in_validation_error_message()
        {
            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "test invalid type",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            await _reviewModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _reviewModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Assign Task 1: Source Type { _reviewModel.PrimaryAssignedTask.AssignedTaskSourceType} does not exist", _reviewModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_entering_an_empty_primary_workspaceAffected_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskSourceType.Add(new AssignedTaskSourceType
            {
                AssignedTaskSourceTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "Simple",
                WorkspaceAffected = "",
                Assessor = "Test User"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            await _reviewModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _reviewModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Assign Task 1: Workspace Affected is required", _reviewModel.ValidationErrorMessages[0]);
        }
        
        [Test]
        public async Task Test_entering_an_empty_primary_assessor_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskSourceType.Add(new AssignedTaskSourceType
            {
                AssignedTaskSourceTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "Simple",
                WorkspaceAffected = "test workspace",
                Assessor = ""
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            await _reviewModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _reviewModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Assign Task 1: Assessor is required", _reviewModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_entering_an_additional_sourcetype_that_does_not_exist_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskSourceType.Add(new AssignedTaskSourceType
            {
                AssignedTaskSourceTypeId = 1,
                Name = "Test entry"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "Test entry",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId, 
                    AssignedTaskSourceType = "This is invalid",
                    WorkspaceAffected = "Test Workspace",
                    Assessor = "Test User"
                }
            };

            await _reviewModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _reviewModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Additional Assign Task: Invalid Source Type - { _reviewModel.AdditionalAssignedTasks[0].AssignedTaskSourceType}", _reviewModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_entering_an_empty_additional_workspaceAffected_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskSourceType.Add(new AssignedTaskSourceType
            {
                AssignedTaskSourceTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    AssignedTaskSourceType = "Simple",
                    WorkspaceAffected = "",
                    Assessor = "Test User"
                }
            };

            await _reviewModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _reviewModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Additional Assign Task: Workspace Affected is required", _reviewModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_entering_an_empty_additional_assessor_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskSourceType.Add(new AssignedTaskSourceType
            {
                AssignedTaskSourceTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    AssignedTaskSourceType = "Simple",
                    WorkspaceAffected = "test workspace",
                    Assessor = ""
                }
            };

            await _reviewModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _reviewModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Additional Assign Task: Assessor is required", _reviewModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_when_primary_assign_task_has_note_it_should_be_copied_to_comments()
        {
            _dbContext.AssignedTaskSourceType.Add(new AssignedTaskSourceType
            {
                AssignedTaskSourceTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User",
                Notes = primaryAssignTaskNote,
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakeUserIdentityService.GetFullNameForUser(A<ClaimsPrincipal>.Ignored)).Returns(Task.FromResult("This Use"));

            await _reviewModel.OnPostDoneAsync(ProcessId, "Done");

            var isExist = await _dbContext.Comment.AnyAsync(c =>
                c.Text.Contains(primaryAssignTaskNote, StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(isExist);
        }


        [Test]
        public async Task Test_when_primary_assign_task_has_no_note_it_should_not_be_copied_to_comments()
        {
            _dbContext.AssignedTaskSourceType.Add(new AssignedTaskSourceType
            {
                AssignedTaskSourceTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                AssignedTaskSourceType = "Simple",
                WorkspaceAffected = "",
                Assessor = "Test User",
                Notes = primaryAssignTaskNote,
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakeUserIdentityService.GetFullNameForUser(A<ClaimsPrincipal>.Ignored)).Returns(Task.FromResult("This Use"));

            var currentCommentsCount = await _dbContext.Comment.CountAsync();

            await _reviewModel.OnPostDoneAsync(ProcessId, "Done");

            var newCommentsCount = await _dbContext.Comment.CountAsync();

            Assert.AreEqual(currentCommentsCount, newCommentsCount);
        }
    }

}
