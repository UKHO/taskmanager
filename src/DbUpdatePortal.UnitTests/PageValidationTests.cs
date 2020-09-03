using DbUpdatePortal.Auth;
using DbUpdatePortal.Enums;
using DbUpdatePortal.Helpers;
using DbUpdatePortal.UnitTests.Helper;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DbUpdatePortal.UnitTests
{
    [TestFixture]
    public class PageValidationTests
    {
        private IDbUpdateUserDbService _fakeDbUpdateUserDbService;
        private PageValidationHelper _pageValidationHelper;
        private DbUpdateWorkflowDbContext _dbContext;
        private AdUser _testUser;
        private AdUser _testUser2;
        private List<AdUser> _validUsers;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<DbUpdateWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new DbUpdateWorkflowDbContext(dbContextOptions);



            _fakeDbUpdateUserDbService = A.Fake<IDbUpdateUserDbService>();

            _pageValidationHelper = new PageValidationHelper(_fakeDbUpdateUserDbService);

            _testUser = AdUserHelper.CreateTestUser(_dbContext);
            _testUser2 = AdUserHelper.CreateTestUser(_dbContext, "Test User", 1);

            _validUsers = new List<AdUser>()
            {
                _testUser, _testUser2
            };

        }

        [TestCase(null)]
        [TestCase("")]
        public void Validation_for_ValidateNewTaskPage_missing_TaskName_fails(string taskName)
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, taskName,
                    "Home waters", "Steady state",
                    "None", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Task Name cannot be empty");

        }

        [TestCase(null)]
        [TestCase("")]
        public void Validation_for_ValidateNewTaskPage_missing_ChartingArea_fails(string chartingArea)
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "New Task",
                    chartingArea, "Steady state",
                    "None", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Charting Area cannot be empty");

        }

        [TestCase(null)]
        [TestCase("")]
        public void Validation_for_ValidateNewTaskPage_missing_UpdateType_fails(string updateType)
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "New Task",
                    "Home waters", updateType,
                    "None", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Update Type cannot be empty");

        }
        [TestCase(null)]
        [TestCase("")]
        public void Validation_for_ValidateNewTaskPage_missing_ProductAction_fails(string productAction)
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "New Task",
                    "Home waters", "Steady state",
                    productAction, validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Product Action Required cannot be empty");

        }

        [Test]
        public void Validation_for_ValidateNewTaskPage_without_Comipler_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var invalidUser = AdUserHelper.CreateTestUser(_dbContext, "Invalid User", 2);

            var taskRole = new TaskRole
            {
                Compiler = null,
                Verifier = _testUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "New Task",
                    "Home waters", "Steady state",
                    "None", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Compiler cannot be empty");
        }

        [Test]
        public void Validation_for_ValidateNewTaskPage_invalid_Verifier_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var invalidUser = AdUserHelper.CreateTestUser(_dbContext, "Invalid User", 2);

            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                Verifier = invalidUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "New Task",
                    "Home waters", "Steady state",
                    "None", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Unable to assign Verifier role to unknown user Invalid User2");
        }

        [Test]
        public void Validation_for_ValidateNewTaskPage_with_mandatory_valid_parameter_passes()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser
            };


            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "New Task",
                    "Home waters", "Steady state",
                    "None", validationErrorMessages);

            Assert.IsTrue(result);
            CollectionAssert.IsEmpty(validationErrorMessages);

        }
        [Test]
        public void Validation_for_ValidateNewTaskPage_with_All_valid_parameter_passes()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                Verifier = _testUser2
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "New Task",
                    "Home waters", "Steady state",
                    "None", validationErrorMessages);
            Assert.IsTrue(result);
            CollectionAssert.IsEmpty(validationErrorMessages);

        }

        [Test]
        public void Validation_for_ValidateWorkflowPage_with_Valid_data_Passes()
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeDbUpdateUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                Verifier = _testUser2
            };

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, "None", DateTime.Now,
                validationErrorMessages);

            Assert.IsTrue(result);
            Assert.IsEmpty(validationErrorMessages);
        }

        [TestCase("", "Valid User1", DbUpdateTaskStageType.Compile)]
        [TestCase("", "Valid User1", DbUpdateTaskStageType.Verify)]
        [TestCase("", "Valid User1", DbUpdateTaskStageType.Verification_Rework)]
        [TestCase("", "Valid User1", DbUpdateTaskStageType.SNC)]
        [TestCase("", "Valid User1", DbUpdateTaskStageType.ENC)]
        [TestCase("", "Valid User1", DbUpdateTaskStageType.Awaiting_Publication)]
        public void Validation_for_ValidateForCompletion_with_empty_user_fails(string assignedUser, string currentUser, DbUpdateTaskStageType stageType)
        {
            var validationErrorMessages = new List<string>();

            var result =
                _pageValidationHelper.ValidateForCompletion(assignedUser, currentUser, stageType, new TaskRole(), null, validationErrorMessages);

            Assert.IsFalse(result);

            CollectionAssert.Contains(validationErrorMessages, "Please assign a user to this stage and Save before completion");

        }

        [TestCase(DbUpdateTaskStageType.Compile)]
        [TestCase(DbUpdateTaskStageType.Verify)]
        [TestCase(DbUpdateTaskStageType.Verification_Rework)]
        [TestCase(DbUpdateTaskStageType.SNC)]
        [TestCase(DbUpdateTaskStageType.ENC)]
        [TestCase(DbUpdateTaskStageType.Awaiting_Publication)]
        public void Validation_for_ValidateForCompletion_with_wrong_user_fails_for_all_stages_other_than_forms(DbUpdateTaskStageType stageType)
        {
            var validationErrorMessages = new List<string>();

            var result =
                _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser2.UserPrincipalName, stageType, new TaskRole(),
                    null, validationErrorMessages);

            Assert.IsFalse(result);
            CollectionAssert.Contains(validationErrorMessages, "Current user is not valid for completion of this task stage");

        }

        [Test]
        public void Validation_for_ValidateForCompletion_fails_for_Compile_step_if_Verifier_not_assigned()
        {
            var validationErrorMessages = new List<string>();

            var role = new TaskRole()
            { Compiler = _testUser };

            var result =
                _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser.UserPrincipalName, DbUpdateTaskStageType.Compile, role,
                    null, validationErrorMessages);

            Assert.IsFalse(result);
            CollectionAssert.Contains(validationErrorMessages, "Please assign a user to Verifier role and Save before completing this stage");

        }

        [TestCase(DbUpdateTaskStageType.Compile)]
        [TestCase(DbUpdateTaskStageType.Verify)]
        public void Validation_for_ValidateForCompletion_Passes_if_user_assigned_for_next_step(DbUpdateTaskStageType currentStage)
        {

            var validationErrorMessages = new List<string>();

            var role = new TaskRole()
            {
                Compiler = _testUser2,
                Verifier = _testUser
            };

            var result = _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser.UserPrincipalName,
                currentStage, role, null, validationErrorMessages);

            Assert.IsTrue(result);

        }

        [TestCase("", "Valid User1", "Please assign a user to this stage before sending this task for Rework")]
        [TestCase("Valid User2", "Valid User1", "Current user is not valid for sending this task for Rework")]
        public void Validation_for_ValidateForRework_with_Invalid_user_fails(string assignedUser, string currentUser, string errorMessage)
        {
            var validationErrorMessages = new List<string>();

            var result =
                _pageValidationHelper.ValidateForRework(assignedUser, currentUser, validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, errorMessage);


        }

        [TestCase("", "Valid User1", "Please assign a user to the Verifier role and Save before completing the workflow")]
        [TestCase("Valid User2", "Valid User1", "Only users assigned to the Verifier role are allowed to complete the workflow.")]
        public void Validation_for_ValidateForCompleteWorkflow_with_Invalid_user_fails(string assignedUser, string currentUser, string errorMessage)
        {
            var validationErrorMessages = new List<string>();

            var result =
                _pageValidationHelper.ValidateForCompleteWorkflow(assignedUser, currentUser, validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, errorMessage);


        }
    }
}
