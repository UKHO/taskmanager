using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common.Helpers.Auth;
using Common.Messages.Events;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Portal.Auth;
using Portal.BusinessLogic;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Pages.DbAssessment;
using Portal.UnitTests.Helpers;
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
        private IWorkflowBusinessLogicService _fakeWorkflowBusinessLogicService;
        private IAdDirectoryService _fakeAdDirectoryService;
        private IPortalUserDbService _fakePortalUserDbService;
        private IPortalUserDbService _realPortalUserDbService;
        private ILogger<ReviewModel> _fakeLogger;
        private IEventServiceApiClient _fakeEventServiceApiClient;
        private ICommentsHelper _commentsHelper;
        private ICommentsHelper _fakeCommentsHelper;
        private IPageValidationHelper _pageValidationHelper;
        private IPageValidationHelper _fakepageValidationHelper;

        public AdUser TestUser { get; set; }

        public AdUser TestUser2 { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory").UseLazyLoadingProxies()
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);
            _realPortalUserDbService = new PortalUserDbService(_dbContext);

            TestUser = AdUserHelper.CreateTestUser(_dbContext);
            TestUser2 = AdUserHelper.CreateTestUser(_dbContext, 2);

            _fakeWorkflowBusinessLogicService = A.Fake<IWorkflowBusinessLogicService>();
            _fakeEventServiceApiClient = A.Fake<IEventServiceApiClient>();
            _commentsHelper = new CommentsHelper(_dbContext, new PortalUserDbService(_dbContext));
            _fakeCommentsHelper = A.Fake<ICommentsHelper>();

            ProcessId = 123;

            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = 123,
                ActivityName = "Review",
                AssessmentData = new AssessmentData(),
                SerialNumber = "123_sn",
                Status = "Started",
                StartedAt = DateTime.Now
            });

            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                ProcessId = ProcessId,
                Reviewer = TestUser
            });

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = ProcessId,
                CorrelationId = Guid.NewGuid()
            });

            _dbContext.OnHold.Add(new OnHold()
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now.AddDays(-1),
                OnHoldBy = TestUser,
                WorkflowInstanceId = 1
            });

            _dbContext.SaveChanges();

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakePortalUserDbService = A.Fake<IPortalUserDbService>();

            _fakeLogger = A.Dummy<ILogger<ReviewModel>>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _fakeAdDirectoryService,
                _fakePortalUserDbService);

            _fakepageValidationHelper = A.Fake<IPageValidationHelper>();

            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _fakeCommentsHelper, _fakeAdDirectoryService, _fakeLogger, _pageValidationHelper,
                _fakePortalUserDbService);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }


        [TestCase("High", "Save", ExpectedResult = true)]
        [TestCase("High", "Done", ExpectedResult = true)]
        [TestCase("Medium", "Save", ExpectedResult = true)]
        [TestCase("Medium", "Done", ExpectedResult = true)]
        [TestCase("Low", "Save", ExpectedResult = true)]
        [TestCase("Low", "Done", ExpectedResult = true)]
        [TestCase("", "Save", ExpectedResult = true)]
        [TestCase("  ", "Save", ExpectedResult = true)]
        [TestCase(null, "Save", ExpectedResult = true)]
        [TestCase("INVALID", "Done", ExpectedResult = false)]
        [TestCase("3454", "Done", ExpectedResult = false)]
        public async Task<bool> Test_CheckReviewPageForErrors_with_valid_and_invalid_complexity_returns_expected_result(string complexity, string action)
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "test workspace",
                Assessor = TestUser,
                Verifier = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();
            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            var valid = await _pageValidationHelper.CheckReviewPageForErrors(action,
                _reviewModel.PrimaryAssignedTask,
                _reviewModel.AdditionalAssignedTasks,
                "dummy team",
                _reviewModel.Reviewer,
                A.Dummy<List<string>>(),
                TestUser.UserPrincipalName,
                TestUser,
                complexity);

            return valid;
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_a_primary_tasktype_that_does_not_exist_results_in_validation_error_message()
        {
            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "test invalid type",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();
            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_reviewModel.ValidationErrorMessages.Contains(
                $"Assign Task 1: Task Type {_reviewModel.PrimaryAssignedTask.TaskType} does not exist"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_an_empty_primary_workspaceAffected_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "",
                Assessor = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();
            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains($"Assign Task 1: Workspace Affected is required"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_an_invalid_primary_assessor_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });

            var testUser = new AdUser { DisplayName = "testing", UserPrincipalName = "test" };

            var reviewData = await _dbContext.DbAssessmentReviewData.FirstAsync(p => p.ProcessId == ProcessId);
            reviewData.Reviewer = testUser;

            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "test workspace",
                Assessor = new AdUser { DisplayName = "Unknown", UserPrincipalName = "unknown" }
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();
            _reviewModel.Reviewer = testUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(testUser))
                .Returns(true);

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("testing", "test"));

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains(
                    $"Assign Task 1: Unable to set Assessor to unknown user Unknown"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_an_invalid_primary_verifier_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "test workspace",
                Assessor = TestUser,
                Verifier = new AdUser
                {
                    DisplayName = "Unknown",
                    UserPrincipalName = "unknown@email.com"
                }
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();
            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains(
                    $"Assign Task 1: Unable to set Verifier to unknown user Unknown"));
        }

        [Test]
        public async Task Test_OnPostDoneAsync_entering_an_empty_primary_assessor_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "test workspace",
                Assessor = AdUser.Empty
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_reviewModel.ValidationErrorMessages.Contains($"Assign Task 1: Assessor is required"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_an_additional_tasktype_that_does_not_exist_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Test entry"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Test entry",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    TaskType = "This is invalid",
                    WorkspaceAffected = "Test Workspace",
                    Assessor = TestUser
                }
            };

            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_reviewModel.ValidationErrorMessages.Contains(
                $"Additional Assign Task: Invalid Task Type - {_reviewModel.AdditionalAssignedTasks[0].TaskType}"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_an_empty_additional_workspaceAffected_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    TaskType = "Simple",
                    WorkspaceAffected = "",
                    Assessor = TestUser
                }
            };
            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains(
                    $"Additional Assign Task: Workspace Affected is required"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_an_invalid_additional_assessor_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.Reviewer = TestUser;
            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    TaskType = "Simple",
                    WorkspaceAffected = "test workspace",
                    Assessor = new AdUser
                    {
                        DisplayName = "Unknown",
                        UserPrincipalName = "unknown@email.com"
                    }
                }
            };

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains(
                    $"Additional Assign Task: Unable to set Assessor to unknown user Unknown"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_an_invalid_additional_verifier_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    TaskType = "Simple",
                    WorkspaceAffected = "test workspace",
                    Assessor = TestUser,
                    Verifier = new AdUser
                    {
                        DisplayName = "Unknown",
                        UserPrincipalName = "unknown@email.com"
                    }
                }
            };
            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains(
                    $"Additional Assign Task: Unable to set Verifier to unknown user Unknown"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_an_empty_additional_assessor_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    TaskType = "Simple",
                    WorkspaceAffected = "test workspace",
                    Assessor = AdUser.Empty
                }
            };
            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains($"Additional Assign Task: Assessor is required"));
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_when_primary_assign_task_has_note_it_should_be_copied_to_comments_not_within_portal()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService,
                _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService, _fakeLogger,
                _fakepageValidationHelper, _fakePortalUserDbService)
            {
                PrimaryAssignedTask = new DbAssessmentReviewData
                {
                    TaskType = "Simple",
                    WorkspaceAffected = "Test Workspace",
                    Assessor = TestUser,
                    Notes = primaryAssignTaskNote,
                    Reviewer = TestUser
                },
                AdditionalAssignedTasks = new List<DbAssessmentAssignTask>()
            };

            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored,
                    _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored,
                    A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<string>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _reviewModel.Reviewer = TestUser;
            _reviewModel.Team = "HW";
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));

            await _reviewModel.OnPostDoneAsync(ProcessId);

            var isExist = await _dbContext.Comments.AnyAsync(c =>
                c.Text.Contains(primaryAssignTaskNote, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(isExist);
        }

        [Test]
        public async Task
            Test_OnPostDoneAsync_when_primary_assign_task_has_no_note_it_should_not_be_copied_to_comments()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote,
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("This User", "thisuser@foobar.com"));

            var currentCommentsCount = await _dbContext.Comments.CountAsync();

            await _reviewModel.OnPostDoneAsync(ProcessId);

            var newCommentsCount = await _dbContext.Comments.CountAsync();

            Assert.AreEqual(currentCommentsCount, newCommentsCount);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_invalid_reviewer_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };
            _reviewModel.Reviewer = new AdUser { DisplayName = "unknown", UserPrincipalName = "unknown" };

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(AdUser.Unknown.UserPrincipalName)).Returns(false);

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Team = "HW";

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Unable to set Reviewer to unknown user {_reviewModel.Reviewer.DisplayName}",
                _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_null_reviewer_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Test entry"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Test entry",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Team = "HW";

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Reviewer cannot be empty", _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_empty_reviewer_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Test entry"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Test entry",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };
            _reviewModel.Reviewer = AdUser.Empty;
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Team = "HW";

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Reviewer cannot be empty", _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_validate_user_not_called_on_reviewer_if_empty()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Test entry"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Test entry",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };
            _reviewModel.Reviewer = AdUser.Empty;
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A.Dummy<string>())).MustNotHaveHappened();
        }


        [Test]
        public async Task Test_OnPostDoneAsync_validate_user_not_called_on_reviewer_if_null()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Test entry"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Test entry",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A.Dummy<string>())).MustNotHaveHappened();
        }


        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_invalid_username_for_reviewer_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(false);

            _reviewModel.Ion = "Ion";
            _reviewModel.ActivityCode = "ActivityCode";
            _reviewModel.SourceCategory = "SourceCategory";
            _reviewModel.Team = "HW";

            _reviewModel.Reviewer = TestUser;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Unable to set Reviewer to unknown user {_reviewModel.Reviewer.DisplayName}",
                _reviewModel.ValidationErrorMessages);
        }


        [Test]
        public async Task
            Test_OnPostDoneAsync_entering_empty_username_for_reviewer_results_in_validation_error_message()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Ion = "Ion";
            _reviewModel.ActivityCode = "ActivityCode";
            _reviewModel.SourceCategory = "SourceCategory";
            _reviewModel.Team = "HW";

            _reviewModel.Reviewer = AdUser.Empty;

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Operators: Reviewer cannot be empty", _reviewModel.ValidationErrorMessages);
        }


        [Test]
        public async Task Test_OnPostDoneAsync_entering_empty_team_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Ion = "Ion";
            _reviewModel.ActivityCode = "ActivityCode";
            _reviewModel.SourceCategory = "SourceCategory";
            _reviewModel.Team = "";

            _reviewModel.Reviewer = TestUser;

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Task Information: Team cannot be empty", _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_That_Task_With_No_Reviewer_Fails_Validation()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });
            await _dbContext.SaveChangesAsync();

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            var row = await _dbContext.DbAssessmentReviewData.FirstAsync();
            row.Reviewer = AdUser.Empty;
            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _reviewModel.Reviewer = TestUser;
            _reviewModel.Team = "HW";

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains(
                "Operators: You are not assigned as the Reviewer of this task. Please assign the task to yourself and click Save",
                _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_That_Task_With_Reviewer_Fails_Validation_If_CurrentUser_Not_Assigned()
        {
            _dbContext.AssignedTaskType.Add(new AssignedTaskType
            {
                AssignedTaskTypeId = 1,
                Name = "Simple"
            });

            await _dbContext.SaveChangesAsync();

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote,
                Reviewer = TestUser
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _reviewModel.Reviewer = TestUser2;
            _reviewModel.Team = "HW";

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains(
                $"Operators: {TestUser.DisplayName} is assigned to this task. Please assign the task to yourself and click Save",
                _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_Given_CurrentUser_Is_Not_Valid_Then_Returns_Validation_Error_Message()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper, _realPortalUserDbService);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote,
                Reviewer = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Reviewer = TestUser2;
            _reviewModel.Team = "HW";

            _dbContext.OnHold.RemoveRange(_dbContext.OnHold.First());
            await _dbContext.SaveChangesAsync();

            var invalidPrincipalName = "THIS-USER-PRINCIPAL-NAME-DOES-NOT-EXIST@example.com";
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("THIS DISPLAY NAME DOES NOT EXIST", invalidPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(invalidPrincipalName))
                .Returns(false);

            var result = (JsonResult)await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains(
                $"Operators: Your user account is not in the correct authorised group. Please contact system administrators",
                _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_Given_CurrentUser_Is_Not_Valid_Then_Returns_Validation_Error_Message()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper, _realPortalUserDbService);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote,
                Reviewer = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Reviewer = TestUser2;
            _reviewModel.Team = "HW";

            _dbContext.OnHold.RemoveRange(_dbContext.OnHold.First());
            await _dbContext.SaveChangesAsync();

            var invalidPrincipalName = "THIS-USER-PRINCIPAL-NAME-DOES-NOT-EXIST@example.com";
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("THIS DISPLAY NAME DOES NOT EXIST", invalidPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(invalidPrincipalName))
                .Returns(false);

            var result = (JsonResult)await _reviewModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains(
                $"Operators: Your user account is not in the correct authorised group. Please contact system administrators",
                _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_That_Setting_Task_To_On_Hold_Creates_A_Row()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper, _realPortalUserDbService);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote,
                Reviewer = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Reviewer = TestUser2;
            _reviewModel.Team = "HW";
            _reviewModel.IsOnHold = true;
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser2.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored,
                    _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored,
                    A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<string>.Ignored))
                .Returns(true);

            _dbContext.OnHold.RemoveRange(_dbContext.OnHold.First());
            await _dbContext.SaveChangesAsync();

            await _reviewModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OnHoldTime);
            Assert.AreEqual(onHoldRow.OnHoldBy.UserPrincipalName, TestUser2.UserPrincipalName);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_That_Setting_Task_To_Off_Hold_Updates_Existing_Row()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper, _realPortalUserDbService);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote,
                Reviewer = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Reviewer = TestUser2;
            _reviewModel.Team = "HW";
            _reviewModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser2.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored,
                    _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored,
                    A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<string>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OffHoldTime);
            Assert.AreEqual(onHoldRow.OffHoldBy.UserPrincipalName, TestUser2.UserPrincipalName);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_That_Setting_Task_To_On_Hold_Adds_Comment()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _commentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper, _fakePortalUserDbService);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote,
                Reviewer = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Reviewer = TestUser2;
            _reviewModel.Team = "HW";
            _reviewModel.IsOnHold = true;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser2.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored,
                    _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored,
                    A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<string>.Ignored))
                .Returns(true);

            _dbContext.OnHold.RemoveRange(_dbContext.OnHold.First());
            await _dbContext.SaveChangesAsync();

            await _reviewModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comments.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} has been put on hold", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task Test_OnPostSaveAsync_That_Setting_Task_To_Off_Hold_Adds_Comment()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _commentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper, _realPortalUserDbService);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = TestUser,
                Notes = primaryAssignTaskNote,
                Reviewer = TestUser
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Reviewer = TestUser2;
            _reviewModel.Team = "HW";
            _reviewModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser2.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored,
                    _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored,
                    A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<string>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comments.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} taken off hold", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task
            Test_OnPostReviewTerminateAsync_Given_CurrentUser_Is_Not_Valid_Then_Returns_Validation_Error_Message()
        {
            var invalidPrincipalName = "THIS-USER-PRINCIPAL-NAME-DOES-NOT-EXIST@example.com";
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("THIS DISPLAY NAME DOES NOT EXIST", invalidPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(invalidPrincipalName))
                .Returns(false);

            var result = (JsonResult)await _reviewModel.OnPostTerminateAsync("Testing", ProcessId);

            // Assert
            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains(
                $"Operators: Your user account is not in the correct authorised group. Please contact system administrators",
                _reviewModel.ValidationErrorMessages);

            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent("ProgressWorkflowInstanceEvent",
                        A<ProgressWorkflowInstanceEvent>.Ignored))
                .MustNotHaveHappened();
            Assert.IsFalse(await _dbContext.WorkflowInstance.AnyAsync(
                wi => wi.ProcessId == ProcessId && wi.Status == WorkflowStatus.Updating.ToString()));
        }

        [Test]
        public async Task
            Test_OnPostReviewTerminateAsync_Given_Task_Not_Assigned_To_User_Terminating_Returns_FailedValidation_Errors()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("ThisUserIsNotTheReviewer", "thisuser@foobar.com"));

            var result = (JsonResult)await _reviewModel.OnPostTerminateAsync("Testing", ProcessId);

            // Assert
            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains(
                "Operators: You are not assigned as the Reviewer of this task. Please assign the task to yourself and click Save",
                _reviewModel.ValidationErrorMessages);

            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent("ProgressWorkflowInstanceEvent",
                        A<ProgressWorkflowInstanceEvent>.Ignored))
                .MustNotHaveHappened();
            Assert.IsFalse(await _dbContext.WorkflowInstance.AnyAsync(
                wi => wi.ProcessId == ProcessId && wi.Status == WorkflowStatus.Updating.ToString()));
        }

        [Test]
        public async Task Test_OnPostReviewTerminateAsync_Updates_WorkflowInstance_With_ActivityChangedAt()
        {
            var processId = 1234;
            var serialNumber = "1234_14";
            var currentActivityChangedAt = DateTime.Today.AddDays(-1);

            await _dbContext.WorkflowInstance.AddAsync(new WorkflowInstance()
            {
                ProcessId = processId,
                SerialNumber = serialNumber,
                ActivityName = WorkflowStage.Review.ToString(),
                ActivityChangedAt = currentActivityChangedAt
            });

            await _dbContext.AssessmentData.AddAsync(new AssessmentData()
            {
                ProcessId = processId,
                PrimarySdocId = 111111
            });


            await _dbContext.DbAssessmentReviewData.AddAsync(new DbAssessmentReviewData()
            {
                ProcessId = processId,
                Reviewer = TestUser
            });

            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            await _reviewModel.OnPostTerminateAsync("Testing", processId);

            // Assert
            var workflowInstance = _dbContext.WorkflowInstance.SingleOrDefault(w => w.ProcessId == processId);

            Assert.IsNotNull(workflowInstance);
            Assert.AreEqual(DateTime.Today, workflowInstance.ActivityChangedAt);

        }

        [Test]
        public async Task Test_OnPostReviewTerminateAsync_Terminating_On_Hold_Task_Results_In_Validation_Error_Message()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            await _reviewModel.OnPostTerminateAsync("Testing", ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains(
                "Task Information: Unable to Terminate task. Take task off hold before terminating and click Save.",
                _reviewModel.ValidationErrorMessages);

        }

        [Test]
        public async Task
            Test_OnPostReviewTerminateAsync_Terminating_Off_Hold_Task_Results_In_No_Validation_Error_Messages()
        {
            var thisOnHold = _dbContext.OnHold.Single(oh => oh.ProcessId == ProcessId);

            thisOnHold.OffHoldTime = DateTime.Now.Date;

            await _dbContext.SaveChangesAsync();

            await _reviewModel.OnPostTerminateAsync("Testing", ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 0);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_Only_Entering_The_Team_Saves_Ok()
        {
            _reviewModel.Reviewer = null;
            _reviewModel.ActivityCode = null;
            _reviewModel.Ion = null;
            _reviewModel.SourceCategory = null;
            _reviewModel.Team = "HW";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "test type",
                WorkspaceAffected = "Test Workspace"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("Test User", "test@email.com"));

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            await _reviewModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual(0, _reviewModel.ValidationErrorMessages.Count);
        }
    }
}
