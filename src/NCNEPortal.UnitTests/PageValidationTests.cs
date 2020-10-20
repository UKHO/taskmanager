using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NCNEPortal.Auth;
using NCNEPortal.Enums;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NCNEWorkflowDatabase.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class PageValidationTests
    {
        private INcneUserDbService _fakeNcneUserDbService;
        private PageValidationHelper _pageValidationHelper;
        private NcneWorkflowDbContext _dbContext;
        private AdUser _testUser;
        private AdUser _testUser2;
        private List<AdUser> _validUsers;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new NcneWorkflowDbContext(dbContextOptions);



            _fakeNcneUserDbService = A.Fake<INcneUserDbService>();

            _pageValidationHelper = new PageValidationHelper(_fakeNcneUserDbService);

            _testUser = AdUserHelper.CreateTestUser(_dbContext);
            _testUser2 = AdUserHelper.CreateTestUser(_dbContext, "Test User", 1);

            _validUsers = new List<AdUser>()
            {
                _testUser, _testUser2
            };


        }


        [TestCase(null)]
        [TestCase("")]
        public void Validation_for_ValidateNewTaskPage_missing_ChartType_fails(string chartType)
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                    .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser
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


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, workflowType, "Adoption", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Workflow Type cannot be empty");

        }

        [TestCase(null)]
        [TestCase("")]
        public void Validation_for_ValidateNewTaskPage_missing_ChartNo_fails_for_Withdrawal(string chartNo)
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, NcneWorkflowType.Withdrawal.ToString(), "Adoption", validationErrorMessages, chartNo);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Chart Number cannot be empty");

        }


        [Test]
        public void Validation_for_ValidateNewTaskPage_invalid_V1_V2_100pCheck_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var invalidUser = AdUserHelper.CreateTestUser(_dbContext, "Invalid User", 2);

            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = invalidUser,
                VerifierTwo = invalidUser,
                HundredPercentCheck = invalidUser
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "NE", "Adoption", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 3);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Unable to assign Verifier1 role to unknown user Invalid User2");
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Unable to assign Verifier2 role to unknown user Invalid User2");
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Unable to assign 100% Check role to unknown user Invalid User2");
        }

        [Test]
        public void Validation_for_ValidateNewTaskPage_with_mandatory_valid_parameter_passes()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser
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


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };

            var result =
                _pageValidationHelper.ValidateNewTaskPage(taskRole, "NC", "Adoption", validationErrorMessages);

            Assert.IsTrue(result);
            CollectionAssert.IsEmpty(validationErrorMessages);

        }

        [Test]
        public void Validation_for_Complete_Step_for_Forms_with_invalid_Repromate_Date_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };


            var result = _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName,
                _testUser.UserPrincipalName,
                NcneTaskStageType.Forms, taskRole, null, null, 1, "Adoption", validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Repromat Date cannot be empty");

        }


        [Test]
        public void Validation_for_Complete_Step_for_Forms_with_invalid_Duration_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };

            DateTime repromatDate = DateTime.Now;
            int Dating = 0;


            var result = _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName,
                _testUser.UserPrincipalName,
                NcneTaskStageType.Forms, taskRole, null, repromatDate, Dating, "Adoption", validationErrorMessages);


            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Duration cannot be empty");

        }

        [Test]
        public void Validation_for_Complete_Step_for_Forms_with_invalid_Publication_Date_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };


            DateTime? publicationDate = null;


            var result = _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName,
                _testUser.UserPrincipalName,
                NcneTaskStageType.Forms, taskRole, publicationDate, null, 1, "Primary", validationErrorMessages);


            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Publication Date cannot be empty");

        }

        [Test]
        public void Validation_for_ValidateWorkflowPage_with_3ps_Expected_return_date_without_sent_to_date_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };

            DateTime? invalidDate = null;



            var ThreePSInfo = (true, invalidDate, DateTime.Now, invalidDate);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, "", "", ThreePSInfo,
                validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "3PS : Please enter date sent to 3PS before entering expected return date");
        }

        [Test]
        public void Validation_for_ValidateWorkflowPage_with_3ps_Actual_return_date_without_sent_to_date_fails()
        {
            var validationErrorMessages = new List<string>();



            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };

            DateTime? invalidDate = null;

            var ThreePSInfo = (true, invalidDate, invalidDate, DateTime.Now);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, "", "", ThreePSInfo,
                validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 2);
            CollectionAssert.Contains(validationErrorMessages, "3PS : Please enter date sent to 3PS before entering actual return date");
            CollectionAssert.Contains(validationErrorMessages, "3PS : Please enter expected return date before entering actual return date");
        }

        [Test]
        public void Validation_for_ValidateWorkflowPage_for_3ps_expected_return_date_earlier_than_sent_to_date_fails()
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };

            DateTime? invalidDate = null;

            var ThreePSInfo = (true, DateTime.Now, DateTime.Now.AddDays(-2), invalidDate);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, "", "", ThreePSInfo,
                validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "3PS : Expected return date cannot be earlier than Sent to 3PS date");

        }

        [Test]
        public void Validation_for_ValidateWorkflowPage_for_3ps_actual_return_date_earlier_than_sent_to_date_fails()
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };

            var ThreePSInfo = (true, DateTime.Now, DateTime.Now, DateTime.Now.AddDays(-2));

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, "", "", ThreePSInfo,
                validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "3PS : Actual return date cannot be earlier than Sent to 3PS date");

        }


        [Test]
        public void Validation_for_ValidateWorkflowPage_with_Valid_data_Passes()
        {
            var validationErrorMessages = new List<string>();

            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = _testUser,
                VerifierOne = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };


            var ThreePSInfo = (true, DateTime.Now, DateTime.Now, DateTime.Now);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, "", "", ThreePSInfo,
                validationErrorMessages);

            Assert.IsTrue(result);
            Assert.IsEmpty(validationErrorMessages);
        }

        [TestCase("", "Valid User1", NcneTaskStageType.With_SDRA)]
        [TestCase("", "Valid User1", NcneTaskStageType.With_Geodesy)]
        [TestCase("", "Valid User1", NcneTaskStageType.Specification)]
        [TestCase("", "Valid User1", NcneTaskStageType.V1)]
        [TestCase("", "Valid User1", NcneTaskStageType.V1_Rework)]
        [TestCase("", "Valid User1", NcneTaskStageType.V2)]
        [TestCase("", "Valid User1", NcneTaskStageType.V2_Rework)]
        [TestCase("", "Valid User1", NcneTaskStageType.Final_Updating)]
        [TestCase("", "Valid User1", NcneTaskStageType.Hundred_Percent_Check)]
        [TestCase("", "Valid User1", NcneTaskStageType.Commit_To_Print)]
        [TestCase("", "Valid User1", NcneTaskStageType.CIS)]
        [TestCase("", "Valid User1", NcneTaskStageType.Publication)]
        [TestCase("", "Valid User1", NcneTaskStageType.Publish_Chart)]
        [TestCase("", "Valid User1", NcneTaskStageType.Clear_Vector)]
        [TestCase("", "Valid User1", NcneTaskStageType.Retire_Old_Version)]
        [TestCase("", "Valid User1", NcneTaskStageType.Consider_Withdrawn_Charts)]
        public void Validation_for_ValidateForCompletion_with_empty_user_fails(string assignedUser, string currentUser, NcneTaskStageType stageType)
        {
            var validationErrorMessages = new List<string>();

            var result =
                _pageValidationHelper.ValidateForCompletion(assignedUser, currentUser, stageType, new TaskRole(), null, null, 0, "NC"
                      , validationErrorMessages);

            Assert.IsFalse(result);

            CollectionAssert.Contains(validationErrorMessages, "Please assign a user to this stage and Save before completion");

        }
        [TestCase(NcneTaskStageType.With_SDRA)]
        [TestCase(NcneTaskStageType.With_Geodesy)]
        [TestCase(NcneTaskStageType.Specification)]
        [TestCase(NcneTaskStageType.V1)]
        [TestCase(NcneTaskStageType.V1_Rework)]
        [TestCase(NcneTaskStageType.V2)]
        [TestCase(NcneTaskStageType.V2_Rework)]
        [TestCase(NcneTaskStageType.Final_Updating)]
        [TestCase(NcneTaskStageType.Hundred_Percent_Check)]
        [TestCase(NcneTaskStageType.Commit_To_Print)]
        [TestCase(NcneTaskStageType.CIS)]
        [TestCase(NcneTaskStageType.Publication)]
        [TestCase(NcneTaskStageType.Publish_Chart)]
        [TestCase(NcneTaskStageType.Clear_Vector)]
        [TestCase(NcneTaskStageType.Retire_Old_Version)]
        [TestCase(NcneTaskStageType.Consider_Withdrawn_Charts)]
        public void Validation_for_ValidateForCompletion_with_wrong_user_fails_for_all_stages_other_than_forms(NcneTaskStageType stageType)
        {
            var validationErrorMessages = new List<string>();

            var result =
                _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser2.UserPrincipalName, stageType, new TaskRole(),
                      null, null, 0, "NC", validationErrorMessages);

            Assert.IsFalse(result);
            CollectionAssert.Contains(validationErrorMessages, "Current user is not valid for completion of this task stage");

        }

        [TestCase("", "Valid User")]
        [TestCase("Valid User", "Another User")]
        public void Validation_for_ValidateForCompletion_for_Forms_Stage_allows_any_user_and_unassigned_to_Complete(string assignedUser, string currentUser)
        {
            var validationErrorMessages = new List<string>();

            var currentStageType = NcneTaskStageType.Forms;

            const int dating = (int)DeadlineEnum.TwoWeeks;

            var result =
                _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser2.UserPrincipalName, currentStageType, new TaskRole(),
                                     DateTime.Now, DateTime.Now, dating, "NC", validationErrorMessages);

            Assert.IsTrue(result);

        }

        [Test]
        public void Validation_for_ValidateForCompletion_fails_for_Compile_step_if_V1_not_assigned()
        {
            var validationErrorMessages = new List<string>();

            var role = new TaskRole()
            { Compiler = _testUser };

            var result =
                _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser.UserPrincipalName, NcneTaskStageType.Compile, role,
                    null, null, 0, "NC", validationErrorMessages);

            Assert.IsFalse(result);
            CollectionAssert.Contains(validationErrorMessages, "Please assign a user to V1 role and Save before completing this stage");

        }
        [Test]
        public void Validation_for_ValidateForCompletion_fails_for_FinalUpdate_step_if_100pCheck_not_assigned()
        {
            var validationErrorMessages = new List<string>();

            var role = new TaskRole()
            {
                Compiler = _testUser,
                VerifierOne = _testUser,
                VerifierTwo = _testUser
            };

            var result =
                _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser.UserPrincipalName, NcneTaskStageType.Final_Updating, role,
                    null, null, 0, "NC", validationErrorMessages);

            Assert.IsFalse(result);
            CollectionAssert.Contains(validationErrorMessages,
                "Please assign a user to 100% Check role and Save before completing this stage");
        }

        [Test]
        public void Validation_for_ValidateForCompletion_fails_for_100pCheck_step_if_V1_not_assigned()
        {
            var validationErrorMessages = new List<string>();

            var role = new TaskRole()
            {
                Compiler = _testUser2,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };

            var result =
                _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser.UserPrincipalName,
                    NcneTaskStageType.Hundred_Percent_Check, role,
                    null, null, 0, "NC", validationErrorMessages);

            Assert.IsFalse(result);
            CollectionAssert.Contains(validationErrorMessages, "Please assign a user to V1 role and Save before completing this stage");
        }

        [TestCase(NcneTaskStageType.Compile)]
        [TestCase(NcneTaskStageType.V1)]
        [TestCase(NcneTaskStageType.V2)]
        public void Validation_for_ValidateForCompletion_Passes_if_user_assigned_for_next_step(NcneTaskStageType currentStage)
        {

            var validationErrorMessages = new List<string>();

            var role = new TaskRole()
            {
                Compiler = _testUser2,
                VerifierOne = _testUser,
                VerifierTwo = _testUser,
                HundredPercentCheck = _testUser2
            };

            var result = _pageValidationHelper.ValidateForCompletion(_testUser.UserPrincipalName, _testUser.UserPrincipalName,
                 currentStage, role, null, null, 0, "NC",
                validationErrorMessages);

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

        [TestCase("", "Valid User1", "Please assign a user to the V1 role and Save before completing the workflow")]
        [TestCase("Valid User2", "Valid User1", "Only users assigned to the V1 role are allowed to complete the workflow.")]
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
