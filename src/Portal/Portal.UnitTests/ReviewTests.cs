using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common.Helpers.Auth;
using FakeItEasy;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Portal.Auth;
using Portal.BusinessLogic;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class ReviewTests
    {
        private WorkflowDbContext _dbContext;
        private HpdDbContext _hpDbContext;
        private ReviewModel _reviewModel;
        private int ProcessId { get; set; }
        private IWorkflowBusinessLogicService _fakeWorkflowBusinessLogicService;
        private IAdDirectoryService _fakeAdDirectoryService;
        private IPortalUserDbService _fakePortalUserDbService;
        private ILogger<ReviewModel> _fakeLogger;
        private IEventServiceApiClient _fakeEventServiceApiClient;
        private ICommentsHelper _commentsHelper;
        private ICommentsHelper _fakeCommentsHelper;
        private IPageValidationHelper _pageValidationHelper;
        private IPageValidationHelper _fakepageValidationHelper;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeWorkflowBusinessLogicService = A.Fake<IWorkflowBusinessLogicService>();
            _fakeEventServiceApiClient = A.Fake<IEventServiceApiClient>();
            _commentsHelper = new CommentsHelper(_dbContext);
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
                Reviewer = "TestUser"
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
                OnHoldUser = "TestUser",
                WorkflowInstanceId = 1
            });

            _dbContext.SaveChanges();

            var hpdDbContextOptions = new DbContextOptionsBuilder<HpdDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _hpDbContext = new HpdDbContext(hpdDbContextOptions);

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakePortalUserDbService = A.Fake<IPortalUserDbService>();

            _fakeLogger = A.Dummy<ILogger<ReviewModel>>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _hpDbContext, _fakeAdDirectoryService, _fakePortalUserDbService);

            _fakepageValidationHelper = A.Fake<IPageValidationHelper>();

            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _fakeCommentsHelper, _fakeAdDirectoryService, _fakeLogger, _pageValidationHelper);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_entering_a_primary_tasktype_that_does_not_exist_results_in_validation_error_message()
        {
            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "test invalid type",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_reviewModel.ValidationErrorMessages.Contains(
                $"Assign Task 1: Task Type {_reviewModel.PrimaryAssignedTask.TaskType} does not exist"));
        }

        [Test]
        public async Task Test_entering_an_empty_primary_workspaceAffected_results_in_validation_error_message()
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
                Assessor = "Test User"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains($"Assign Task 1: Workspace Affected is required"));
        }

        [Test]
        public async Task Test_entering_an_empty_primary_assessor_results_in_validation_error_message()
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
                Assessor = ""
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_reviewModel.ValidationErrorMessages.Contains($"Assign Task 1: Assessor is required"));
        }

        [Test]
        public async Task Test_entering_an_additional_tasktype_that_does_not_exist_results_in_validation_error_message()
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
                Assessor = "Test User"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    TaskType = "This is invalid",
                    WorkspaceAffected = "Test Workspace",
                    Assessor = "Test User"
                }
            };

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_reviewModel.ValidationErrorMessages.Contains(
                $"Additional Assign Task: Invalid Task Type - {_reviewModel.AdditionalAssignedTasks[0].TaskType}"));
        }

        [Test]
        public async Task Test_entering_an_empty_additional_workspaceAffected_results_in_validation_error_message()
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
                Assessor = "Test User"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    TaskType = "Simple",
                    WorkspaceAffected = "",
                    Assessor = "Test User"
                }
            };

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains(
                    $"Additional Assign Task: Workspace Affected is required"));
        }

        [Test]
        public async Task Test_entering_an_empty_additional_assessor_results_in_validation_error_message()
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
                Assessor = "Test User"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>
            {
                new DbAssessmentAssignTask
                {
                    ProcessId = ProcessId,
                    TaskType = "Simple",
                    WorkspaceAffected = "test workspace",
                    Assessor = ""
                }
            };

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(
                _reviewModel.ValidationErrorMessages.Contains($"Additional Assign Task: Assessor is required"));
        }

        [Test]
        public async Task Test_when_primary_assign_task_has_note_it_should_be_copied_to_comments_not_within_portal()
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
                _fakepageValidationHelper)
            {
                PrimaryAssignedTask = new DbAssessmentReviewData
                {
                    TaskType = "Simple",
                    WorkspaceAffected = "Test Workspace",
                    Assessor = "Test User",
                    Notes = primaryAssignTaskNote,
                    Reviewer = "TestUser"
                },
                AdditionalAssignedTasks = new List<DbAssessmentAssignTask>()
            };

            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored, _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _reviewModel.Reviewer = "TestUser";
            _reviewModel.Team = "Home Waters";
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetailsAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(("TestUser2", "testuser2@foobar.com")));

            await _reviewModel.OnPostDoneAsync(ProcessId);

            var isExist = await _dbContext.Comment.AnyAsync(c =>
                c.Text.Contains(primaryAssignTaskNote, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(isExist);
        }

        [Test]
        public async Task Test_when_primary_assign_task_has_no_note_it_should_not_be_copied_to_comments()
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
                Assessor = "Test User",
                Notes = primaryAssignTaskNote,
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetailsAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(("This User", "thisuser@foobar.com")));

            var currentCommentsCount = await _dbContext.Comment.CountAsync();

            await _reviewModel.OnPostDoneAsync(ProcessId);

            var newCommentsCount = await _dbContext.Comment.CountAsync();

            Assert.AreEqual(currentCommentsCount, newCommentsCount);
        }

        [Test]
        public async Task Test_invalid_reviewer_results_in_validation_error_message()
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
                Assessor = "Test User"
            };
            _reviewModel.Reviewer = "Invalid User";
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync("Invalid User")).Returns(false);

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Team = "Home Waters";

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Unable to set Reviewer to unknown user {_reviewModel.Reviewer}",
                _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_null_reviewer_results_in_validation_error_message()
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
                Assessor = "Test User"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Team = "Home Waters";

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Reviewer cannot be empty", _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_empty_reviewer_results_in_validation_error_message()
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
                Assessor = "Test User"
            };
            _reviewModel.Reviewer = "";
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Team = "Home Waters";

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Reviewer cannot be empty", _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_validate_user_not_called_on_reviewer_if_empty()
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
                Assessor = "Test User"
            };
            _reviewModel.Reviewer = "";
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            await _reviewModel.OnPostDoneAsync(ProcessId);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A.Dummy<string>())).MustNotHaveHappened();
        }


        [Test]
        public async Task Test_validate_user_not_called_on_reviewer_if_null()
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
                Assessor = "Test User"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            await _reviewModel.OnPostDoneAsync(ProcessId);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A.Dummy<string>())).MustNotHaveHappened();
        }


        [Test]
        public async Task Test_entering_invalid_username_for_reviewer_results_in_validation_error_message()
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
                Assessor = "Test User"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(false);

            _reviewModel.Ion = "Ion";
            _reviewModel.ActivityCode = "ActivityCode";
            _reviewModel.SourceCategory = "SourceCategory";
            _reviewModel.Team = "Home Waters";

            _reviewModel.Reviewer = "TestUser";

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Unable to set Reviewer to unknown user {_reviewModel.Reviewer}",
                _reviewModel.ValidationErrorMessages);
        }


        [Test]
        public async Task Test_entering_empty_username_for_reviewer_results_in_validation_error_message()
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
                Assessor = "Test User"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Ion = "Ion";
            _reviewModel.ActivityCode = "ActivityCode";
            _reviewModel.SourceCategory = "SourceCategory";
            _reviewModel.Team = "Home Waters";

            _reviewModel.Reviewer = "";

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Operators: Reviewer cannot be empty", _reviewModel.ValidationErrorMessages);
        }


        [Test]
        public async Task Test_entering_empty_team_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
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
                Assessor = "Test User"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            _reviewModel.Ion = "Ion";
            _reviewModel.ActivityCode = "ActivityCode";
            _reviewModel.SourceCategory = "SourceCategory";
            _reviewModel.Team = "";

            _reviewModel.Reviewer = "TestUser";

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Task Information: Team cannot be empty", _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_That_Task_With_No_Reviewer_Fails_Validation_On_Done()
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
                Assessor = "Test User",
                Notes = primaryAssignTaskNote
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            var row = await _dbContext.DbAssessmentReviewData.FirstAsync();
            row.Reviewer = "";
            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _reviewModel.Reviewer = "TestUser";
            _reviewModel.Team = "Home Waters";

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetailsAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(("TestUser2", "testuser2@foobar.com")));

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Operators: You are not assigned as the Reviewer of this task. Please assign the task to yourself and click Save", _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_That_Task_With_Reviewer_Fails_Validation_If_CurrentUser_Not_Assigned_At_Done()
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
                Assessor = "Test User",
                Notes = primaryAssignTaskNote,
                Reviewer = "TestUser"
            };
            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _reviewModel.Reviewer = "TestUser";
            _reviewModel.Team = "Home Waters";

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetailsAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(("TestUser2", "testuser2@foobar.com")));

            await _reviewModel.OnPostDoneAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Operators: TestUser is assigned to this task. Please assign the task to yourself and click Save", _reviewModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_That_Setting_Task_To_On_Hold_Creates_A_Row()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User",
                Notes = primaryAssignTaskNote,
                Reviewer = "TestUser"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _reviewModel.Reviewer = "TestUser2";
            _reviewModel.Team = "Home Waters";
            _reviewModel.IsOnHold = true;
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetailsAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(("TestUser2", "testuser2@foobar.com")));
            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored, _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(true);

            _dbContext.OnHold.RemoveRange(_dbContext.OnHold.First());
            await _dbContext.SaveChangesAsync();

            await _reviewModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OnHoldTime);
            Assert.AreEqual(onHoldRow.OnHoldUser, "TestUser2");
        }

        [Test]
        public async Task Test_That_Setting_Task_To_Off_Hold_Updates_Existing_Row()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User",
                Notes = primaryAssignTaskNote,
                Reviewer = "TestUser"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _reviewModel.Reviewer = "TestUser2";
            _reviewModel.Team = "Home Waters";
            _reviewModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetailsAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(("TestUser2", "testuser2@foobar.com")));
            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored, _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OffHoldTime);
            Assert.AreEqual(onHoldRow.OffHoldUser, "TestUser2");
        }

        [Test]
        public async Task Test_That_Setting_Task_To_On_Hold_Adds_Comment()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _commentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User",
                Notes = primaryAssignTaskNote,
                Reviewer = "TestUser"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _reviewModel.Reviewer = "TestUser2";
            _reviewModel.Team = "Home Waters";
            _reviewModel.IsOnHold = true;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetailsAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(("TestUser2", "testuser2@foobar.com")));
            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored, _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(true);

            _dbContext.OnHold.RemoveRange(_dbContext.OnHold.First());
            await _dbContext.SaveChangesAsync();

            await _reviewModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comment.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} has been put on hold", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task Test_That_Setting_Task_To_Off_Hold_Adds_Comment()
        {
            _reviewModel = new ReviewModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _commentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakepageValidationHelper);

            var primaryAssignTaskNote = "Testing primary";

            _reviewModel.PrimaryAssignedTask = new DbAssessmentReviewData
            {
                TaskType = "Simple",
                WorkspaceAffected = "Test Workspace",
                Assessor = "Test User",
                Notes = primaryAssignTaskNote,
                Reviewer = "TestUser"
            };

            _reviewModel.AdditionalAssignedTasks = new List<DbAssessmentAssignTask>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _reviewModel.Reviewer = "TestUser2";
            _reviewModel.Team = "Home Waters";
            _reviewModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetailsAsync(A<ClaimsPrincipal>.Ignored))
                .Returns(Task.FromResult(("TestUser2", "testuser2@foobar.com")));
            A.CallTo(() => _fakepageValidationHelper.CheckReviewPageForErrors(A<string>.Ignored, _reviewModel.PrimaryAssignedTask, A<List<DbAssessmentAssignTask>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(true);

            await _reviewModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comment.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} taken off hold", StringComparison.OrdinalIgnoreCase)));
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

            await _dbContext.SaveChangesAsync();

            await _reviewModel.OnPostReviewTerminateAsync("Testing", processId);

            // Assert
            var workflowInstance = _dbContext.WorkflowInstance.SingleOrDefault(w => w.ProcessId == processId);

            Assert.IsNotNull(workflowInstance);
            Assert.AreEqual(DateTime.Today, workflowInstance.ActivityChangedAt);

        }

        [Test]
        public async Task Test_Terminating_On_Hold_Task_Results_In_Validation_Error_Message()
        {
            await _reviewModel.OnPostValidateTerminateAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Unable to Terminate task. Take task off hold before terminating.", _reviewModel.ValidationErrorMessages);

        }

        [Test]
        public async Task Test_Terminating_Off_Hold_Task_Results_In_No_Validation_Error_Messages()
        {
            var thisOnHold = _dbContext.OnHold.Single(oh => oh.ProcessId == ProcessId);

            thisOnHold.OffHoldTime = DateTime.Now.Date;

            await _dbContext.SaveChangesAsync();

            await _reviewModel.OnPostValidateTerminateAsync(ProcessId);

            Assert.GreaterOrEqual(_reviewModel.ValidationErrorMessages.Count, 0);
        }
    }
}
