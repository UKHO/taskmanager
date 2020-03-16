using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NCNEPortal.Auth;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NUnit.Framework;
using System.Collections.Generic;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class PageValidationTests
    {
        private NcneWorkflowDbContext _dbContext;
        private IUserIdentityService _fakeUserIdentityService;
        private IDirectoryService _fakeDirectoryService;
        private PageValidationHelper _pageValidationHelper;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new NcneWorkflowDbContext(dbContextOptions);

            _fakeUserIdentityService = A.Fake<IUserIdentityService>();
            _fakeDirectoryService = A.Fake<IDirectoryService>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _fakeUserIdentityService, _fakeDirectoryService);

        }


        [TestCase(null)]
        [TestCase("")]
        public void Validation_for_ValidateNewTaskPage_missing_ChartType_fails(string chartType)
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDirectoryService.GetGroupMembers())
                    .Returns(new List<string> { "Valid User1", "Valid User2" });


            var taskRole = new TaskRole
            {
                Compiler = "Valid User1"
            };

            var result =
                    _pageValidationHelper.ValidateNewTaskPage(taskRole, "NC", chartType, validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Chart Type cannot be empty");

        }

        [TestCase(null)]
        [TestCase("")]
        public void Validation_for_ValidateNewTaskPage_missing_WorkflowType_fails(string workflowType)
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDirectoryService.GetGroupMembers())
                .Returns(new List<string> { "Valid User1", "Valid User2" });


            var taskRole = new TaskRole
            {
                Compiler = "Valid User1"
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, workflowType, "Adoption", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Workflow Type cannot be empty");

        }

        [TestCase(null, "Task Information: Compiler cannot be empty")]
        [TestCase("", "Task Information: Compiler cannot be empty")]
        [TestCase("Invalid User", "Task Information: Unable to assign Compiler role to unknown user Invalid User")]
        public void Validation_for_ValidateNewTaskPage_missing_or_invalid_Compiler_fails(string username, string expectedErrorMessage)
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDirectoryService.GetGroupMembers())
                .Returns(new List<string> { "Valid User1", "Valid User2" });


            var taskRole = new TaskRole
            {
                Compiler = username
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "NE", "Adoption", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, expectedErrorMessage);
        }

        [Test]
        public void Validation_for_ValidateNewTaskPage_invalid_V1_V2_Publisher_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDirectoryService.GetGroupMembers())
                .Returns(new List<string> { "Valid User1", "Valid User2" });


            var taskRole = new TaskRole
            {
                Compiler = "Valid User1",
                VerifierOne = "InValid User",
                VerifierTwo = "InValid User",
                Publisher = "InValid User"
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "NE", "Adoption", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 3);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Unable to assign Verifier1 role to unknown user InValid User");
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Unable to assign Verifier2 role to unknown user InValid User");
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Unable to assign Publisher role to unknown user InValid User");
        }

        [Test]
        public void Validation_for_ValidateNewTaskPage_with_mandatory_valid_parameter_passes()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDirectoryService.GetGroupMembers())
                .Returns(new List<string> { "Valid User1", "Valid User2" });


            var taskRole = new TaskRole
            {
                Compiler = "Valid User1"
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "NC", "Adoption", validationErrorMessages);

            Assert.IsTrue(result);
            CollectionAssert.IsEmpty(validationErrorMessages);

        }

        [Test]
        public void Validation_for_ValidateNewTaskPage_with_All_valid_parameter_passes()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeDirectoryService.GetGroupMembers())
                .Returns(new List<string> { "Valid User1", "Valid User2" });


            var taskRole = new TaskRole
            {
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "NC", "Adoption", validationErrorMessages);

            Assert.IsTrue(result);
            CollectionAssert.IsEmpty(validationErrorMessages);

        }
    }
}
