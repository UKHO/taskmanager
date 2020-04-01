﻿using Common.Helpers.Auth;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NCNEPortal.Auth;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class PageValidationTests
    {
        private NcneWorkflowDbContext _dbContext;
        private IAdDirectoryService _fakeAdDirectoryService;
        private INcneUserDbService _fakeNcneUserDbService;
        private PageValidationHelper _pageValidationHelper;
        private List<AdUser> _validUsers;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new NcneWorkflowDbContext(dbContextOptions);

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakeNcneUserDbService = A.Fake<INcneUserDbService>();

            _pageValidationHelper = new PageValidationHelper(_fakeNcneUserDbService);

            _validUsers = new List<AdUser>
            {
                new AdUser
                {
                    DisplayName = "Valid User1"
                },
                new AdUser
                {
                    DisplayName = "Valid User2"
                }
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


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


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


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

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


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

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


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


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


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


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

        [Test]
        public void Validation_for_ValidateWorkflowPage_with_invalid_Repromate_Date_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);

            var taskRole = new TaskRole
            {
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            DateTime? nulldate = null;


            var ThreePSInfo = (false, nulldate, nulldate, nulldate);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, null, null, 1, "Adoption", ThreePSInfo,
                validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Repromat Date cannot be empty");

        }


        [Test]
        public void Validation_for_ValidateWorkflowPage_with_invalid_Duration_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            DateTime? nulldate = null;

            DateTime repromatDate = DateTime.Now;
            int Dating = 0;


            var ThreePSInfo = (false, nulldate, nulldate, nulldate);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, null, repromatDate, Dating, "Adoption", ThreePSInfo,
                validationErrorMessages);

            Assert.IsFalse(result);
            Assert.AreEqual(validationErrorMessages.Count, 1);
            CollectionAssert.Contains(validationErrorMessages, "Task Information: Duration cannot be empty");

        }

        [Test]
        public void Validation_for_ValidateWorkflowPage_with_invalid_Publication_Date_fails()
        {
            var validationErrorMessages = new List<string>();


            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync())
                .Returns(_validUsers);


            var taskRole = new TaskRole
            {
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            DateTime? invalidDate = null;

            DateTime? publicationDate = null;



            var ThreePSInfo = (false, invalidDate, invalidDate, invalidDate);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, publicationDate, null, 1, "Primary", ThreePSInfo,
                validationErrorMessages);

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
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            DateTime? invalidDate = null;

            DateTime? publicationDate = null;



            var ThreePSInfo = (true, invalidDate, DateTime.Now, invalidDate);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, publicationDate, DateTime.Now, 1, "Adoption", ThreePSInfo,
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
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            DateTime? invalidDate = null;

            DateTime? publicationDate = null;



            var ThreePSInfo = (true, invalidDate, invalidDate, DateTime.Now);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, publicationDate, DateTime.Now, 1, "Adoption", ThreePSInfo,
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
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            DateTime? invalidDate = null;

            DateTime? publicationDate = null;

            var ThreePSInfo = (true, DateTime.Now, DateTime.Now.AddDays(-2), invalidDate);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, publicationDate, DateTime.Now, 1, "Adoption", ThreePSInfo,
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
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            DateTime? publicationDate = null;

            var ThreePSInfo = (true, DateTime.Now, DateTime.Now, DateTime.Now.AddDays(-2));

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, publicationDate, DateTime.Now, 1, "Adoption", ThreePSInfo,
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
                Compiler = "Valid User1",
                VerifierOne = "Valid User2",
                VerifierTwo = "Valid User1",
                Publisher = "Valid User2"
            };

            DateTime? publicationDate = null;

            var ThreePSInfo = (true, DateTime.Now, DateTime.Now, DateTime.Now);

            var result = _pageValidationHelper.ValidateWorkflowPage(taskRole, publicationDate, DateTime.Now, 1, "Adoption", ThreePSInfo,
                validationErrorMessages);

            Assert.IsTrue(result);
            Assert.IsEmpty(validationErrorMessages);
        }

    }
}
