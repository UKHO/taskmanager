using DbUpdatePortal.Auth;
using DbUpdatePortal.Helpers;
using DbUpdatePortal.UnitTests.Helper;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
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
    }
}
