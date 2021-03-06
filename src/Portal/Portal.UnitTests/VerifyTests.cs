﻿using Common.Helpers;
using Common.Helpers.Auth;
using Common.Messages.Events;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Auth;
using Portal.BusinessLogic;
using Portal.Configuration;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Pages.DbAssessment;
using Portal.UnitTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class VerifyTests
    {
        private WorkflowDbContext _dbContext;
        private VerifyModel _verifyModel;
        private int ProcessId { get; set; }
        private IWorkflowBusinessLogicService _fakeWorkflowBusinessLogicService;
        private ILogger<VerifyModel> _fakeLogger;
        private IEventServiceApiClient _fakeEventServiceApiClient;
        private IAdDirectoryService _fakeAdDirectoryService;
        private IPortalUserDbService _fakePortalUserDbService;
        private IPortalUserDbService _realPortalUserDbService;
        private ICommentsHelper _commentsHelper;
        private ICommentsHelper _fakeCommentsHelper;
        private IPageValidationHelper _pageValidationHelper;
        private IPageValidationHelper _fakePageValidationHelper;
        private ICarisProjectHelper _fakeCarisProjectHelper;
        private IOptions<GeneralConfig> _generalConfig;
        public AdUser TestUser { get; set; }
        public AdUser TestUser2 { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);
            _realPortalUserDbService = new PortalUserDbService(_dbContext);

            _fakeWorkflowBusinessLogicService = A.Fake<IWorkflowBusinessLogicService>();
            _fakeEventServiceApiClient = A.Fake<IEventServiceApiClient>();
            _fakeCarisProjectHelper = A.Fake<ICarisProjectHelper>();
            _fakePageValidationHelper = A.Fake<IPageValidationHelper>();
            _generalConfig = A.Fake<IOptions<GeneralConfig>>();

            TestUser = AdUserHelper.CreateTestUser(_dbContext);
            TestUser2 = AdUserHelper.CreateTestUser(_dbContext, 2);

            ProcessId = 123;

            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                WorkflowInstanceId = 1,
                ProcessId = ProcessId,
                ActivityName = "Verify",
                SerialNumber = "123_456"
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = ProcessId
            });

            _dbContext.DbAssessmentVerifyData.Add(new DbAssessmentVerifyData()
            {
                ProcessId = ProcessId,
                Assessor = TestUser,
                Verifier = TestUser
            });

            _dbContext.ProductAction.Add(new ProductAction
            {
                ProcessId = ProcessId,
                ImpactedProduct = "",
                ProductActionType = new ProductActionType { Name = "Test" },
                Verified = false
            });

            _dbContext.SncAction.Add(new SncAction
            {
                ProcessId = ProcessId,
                SncActionType = new SncActionType { Name = "test" },
                Verified = false
            });

            _dbContext.DataImpact.Add(new DataImpact
            {
                ProcessId = ProcessId
            });

            _dbContext.SaveChanges();

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakePortalUserDbService = A.Fake<IPortalUserDbService>();
            _commentsHelper = new CommentsHelper(_dbContext, _fakePortalUserDbService);
            _fakeCommentsHelper = A.Fake<ICommentsHelper>();

            _fakeLogger = A.Dummy<ILogger<VerifyModel>>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _fakeAdDirectoryService, _fakePortalUserDbService);

            _verifyModel = new VerifyModel(_dbContext,
                _fakeWorkflowBusinessLogicService,
                _fakeEventServiceApiClient,
                _fakeCommentsHelper,
                _fakeAdDirectoryService,
                _fakeLogger,
                _pageValidationHelper,
                _fakeCarisProjectHelper,
                _generalConfig,
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
        public async Task<bool> Test_CheckVerifyPageForErrors_with_valid_and_invalid_complexity_returns_expected_result(string complexity, string action)
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser))
                .Returns(true);

            var valid = await _pageValidationHelper.CheckVerifyPageForErrors(
                action,
                "ion",
                complexity,
                "activity",
                "source category",
                TestUser,
                A.Dummy<bool>(),
                A.Dummy<string>(),
                A.Dummy<List<ProductAction>>(),
                A.Dummy<bool>(),
                A.Dummy<string>(),
                A.Dummy<List<SncAction>>(),
                A.Dummy<List<DataImpact>>(),
                A.Dummy<DataImpact>(),
                "team",
                A.Dummy<List<string>>(),
                TestUser.UserPrincipalName,
                TestUser,
                A.Dummy<bool>());


            return valid;
        }

        [Test]
        public async Task Test_OnPostSaveAsync_entering_an_empty_ion_activityCode_sourceCategory_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "";
            _verifyModel.ActivityCode = "";
            _verifyModel.SourceCategory = "";

            _verifyModel.Verifier = TestUser;
            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.Contains($"Task Information: Ion cannot be empty", _verifyModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Activity code cannot be empty", _verifyModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Source category cannot be empty", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_entering_an_empty_Verifier_results_in_validation_error_message()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Verifier cannot be empty", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_entering_invalid_username_for_verifier_results_in_validation_error_message()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";

            _verifyModel.Verifier = TestUser;

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Unable to set Verifier to unknown user {_verifyModel.Verifier.DisplayName}", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_entering_duplicate_hpd_usages_in_dataImpact_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = TestUser;
            var hpdUsage = new HpdUsage()
            {
                HpdUsageId = 1,
                Name = "HpdUsageName"
            };
            _verifyModel.DataImpacts = new List<DataImpact>()
            {
                new DataImpact() { DataImpactId = 1, HpdUsageId = 1, HpdUsage = hpdUsage, ProcessId = 123},
                new DataImpact() {DataImpactId = 2, HpdUsageId = 1, HpdUsage = hpdUsage, ProcessId = 123}
            };

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Data Impact: More than one of the same Usage selected", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_entering_duplicate_impactedProducts_in_productAction_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = TestUser;
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "Some change details";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1234", ProductActionTypeId = 1}
            };

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record Enc Action: More than one of the same Enc Impacted Products selected", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_entering_duplicate_impactedProducts_in_sncAction_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = TestUser;
            _verifyModel.SncActioned = true;
            _verifyModel.SncActionChangeDetails = "Some change details";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordSncAction = new List<SncAction>
            {
                new SncAction() { SncActionId = 1, ImpactedProduct = "GB1234", SncActionTypeId = 1},
                new SncAction() { SncActionId = 2, ImpactedProduct = "GB1234", SncActionTypeId = 1}
            };

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record Snc Action: More than one of the same Snc Impacted Products selected", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_entering_invalid_impactedProducts_in_productAction_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = TestUser;
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "Some change details";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1}
            };

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 2);
            Assert.Contains($"Record Enc Action: Impacted Product {_verifyModel.RecordProductAction[0].ImpactedProduct} does not exist", _verifyModel.ValidationErrorMessages);
            Assert.Contains($"Record Enc Action: Impacted Product {_verifyModel.RecordProductAction[1].ImpactedProduct} does not exist", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_Given_CurrentUser_Is_Not_Valid_Then_Returns_Validation_Error_Message()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _realPortalUserDbService);

            var invalidPrincipalName = "THIS-USER-PRINCIPAL-NAME-DOES-NOT-EXIST@example.com";
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("THIS DISPLAY NAME DOES NOT EXIST", invalidPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(invalidPrincipalName))
                .Returns(false);

            var result = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, result.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Your user account is not in the correct authorised group. Please contact system administrators",
                _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_Given_CurrentUser_Is_Not_Valid_Then_Returns_Validation_Error_Message()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _realPortalUserDbService);

            var invalidPrincipalName = "THIS-USER-PRINCIPAL-NAME-DOES-NOT-EXIST@example.com";
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("THIS DISPLAY NAME DOES NOT EXIST", invalidPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(invalidPrincipalName))
                .Returns(false);

            var result = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, result.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Your user account is not in the correct authorised group. Please contact system administrators",
                _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_unverified_productactions_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = TestUser;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1}
            };

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record ENC Action: All ENC Actions must be verified", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_unverified_sncactions_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = TestUser;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordSncAction = new List<SncAction>
            {
                new SncAction() { SncActionId = 1, ImpactedProduct = "GB1234", SncActionTypeId = 1},
                new SncAction() { SncActionId = 2, ImpactedProduct = "GB1234", SncActionTypeId = 1}
            };

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record SNC Action: All SNC Actions must be verified", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_features_unverified_on_dataimpacts_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = TestUser;

            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };

            var hpdUsage1 = new HpdUsage()
            {
                HpdUsageId = 1,
                Name = "HpdUsageName1"
            };
            var hpdUsage2 = new HpdUsage()
            {
                HpdUsageId = 2,
                Name = "HpdUsageName2"
            };
            _verifyModel.DataImpacts = new List<DataImpact>()
            {
                new DataImpact() { DataImpactId = 1, HpdUsageId = 1, HpdUsage = hpdUsage1, ProcessId = 123},
                new DataImpact() {DataImpactId = 2, HpdUsageId = 2, HpdUsage = hpdUsage2, ProcessId = 123}
            };

            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.WarningsDetected, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Data Impact: There are incomplete Features Verified tick boxes.", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_unverified_features_on_empty_dataimpacts_then_no_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;

            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };

            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.StsDataImpact = new DataImpact() { HpdUsageId = 1 };

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreNotEqual((int)VerifyCustomHttpStatusCode.WarningsDetected, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.False(_verifyModel.ValidationErrorMessages.Contains("Data Impact: There are incomplete Features Verified tick boxes."));
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_stsdataimpact_usage_not_selected_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;

            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };

            _verifyModel.DataImpacts = new List<DataImpact>();

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.WarningsDetected, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Data Impact: STS Usage has not been selected, are you sure you want to continue?", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_stsdataimpact_usage_selected_and_verified_false_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser", "testuser@foobar.com"));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;

            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };

            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.StsDataImpact = new DataImpact() { HpdUsageId = 1, FeaturesVerified = false };

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Data Impact: STS Usage has not been Verified", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_given_stsdataimpact_usage_selected_and_verified_false_then_validation_error_message_is_not_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.RecordSncAction = new List<SncAction>
            {
                new SncAction() { SncActionId = 1, ImpactedProduct = "GB1234", SncActionTypeId = 1,Verified = true},
                new SncAction() { SncActionId = 2, ImpactedProduct = "GB1235", SncActionTypeId = 1, Verified = true}
            };

            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };


            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.StsDataImpact = new DataImpact() { HpdUsageId = 1, FeaturesVerified = false };

            var response = (StatusCodeResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_Given_StsDataUsage_Has_Invalid_HpdUsageId_Then_No_Record_Is_Saved()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.RecordSncAction = new List<SncAction>();

            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };

            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.StsDataImpact = new DataImpact() { HpdUsageId = 0 };

            var response = (StatusCodeResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode);

            var stsDataImpact = await _dbContext.DataImpact.SingleOrDefaultAsync(di => di.ProcessId == ProcessId && di.StsUsage);

            Assert.IsNull(stsDataImpact);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_Given_StsDataUsage_Has_Valid_HpdUsageId_Then_Record_Is_Saved()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1234" });
            _dbContext.CachedHpdEncProduct.Add(new CachedHpdEncProduct() { Name = "GB1235" });
            await _dbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.RecordSncAction = new List<SncAction>();

            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };

            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.StsDataImpact = new DataImpact()
            {
                ProcessId = ProcessId,
                HpdUsageId = 2,
                FeaturesVerified = false,
                Comments = "This is a test comment",
                //The following properties should be overwritten
                FeaturesSubmitted = true,
                Edited = true,
                DataImpactId = 99999,
                StsUsage = false
            };

            var response = (StatusCodeResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode);

            var stsDataImpact = await _dbContext.DataImpact.SingleOrDefaultAsync(di => di.ProcessId == ProcessId && di.StsUsage);

            Assert.IsNotNull(stsDataImpact);

            Assert.AreEqual(ProcessId, stsDataImpact.ProcessId);
            Assert.AreEqual(_verifyModel.StsDataImpact.HpdUsageId, stsDataImpact.HpdUsageId);
            Assert.AreEqual(_verifyModel.StsDataImpact.Comments, stsDataImpact.Comments);
            Assert.AreEqual(_verifyModel.StsDataImpact.FeaturesVerified, stsDataImpact.FeaturesVerified);

            Assert.IsFalse(stsDataImpact.FeaturesSubmitted);
            Assert.IsFalse(stsDataImpact.Edited);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_entering_empty_team_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "";

            _verifyModel.Verifier = TestUser;

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Task Information: Team cannot be empty", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_has_active_child_tasks_returns_warning_message()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";

            _verifyModel.Verifier = TestUser;

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            var childProcessId = 555;

            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                WorkflowInstanceId = 2,
                ProcessId = childProcessId,
                ActivityName = "Verify",
                SerialNumber = "555_456",
                ParentProcessId = ProcessId,
                Status = WorkflowStatus.Started.ToString()

            });

            await _dbContext.SaveChangesAsync();

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.WarningsDetected, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_verifyModel.ValidationErrorMessages.Any(v => v.Contains(childProcessId.ToString())));
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_caris_project_is_not_created_then_MarkCarisProjectAsComplete_is_not_called()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";

            _verifyModel.Verifier = TestUser;

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = ProcessId,
                CorrelationId = Guid.NewGuid()
            });

            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            A.CallTo(() => _fakeCarisProjectHelper.MarkCarisProjectAsComplete(A<int>.Ignored, A<int>.Ignored)).MustNotHaveHappened();

        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_caris_project_is_created_then_MarkCarisProjectAsComplete_is_called()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";

            _verifyModel.Verifier = TestUser;

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = ProcessId,
                CorrelationId = Guid.NewGuid()
            });

            _dbContext.CarisProjectDetails.Add(new CarisProjectDetails()
            {
                ProcessId = ProcessId,
                ProjectId = 123
            });

            _dbContext.HpdUser.Add(new HpdUser()
            {
                AdUser = TestUser,
                HpdUsername = "TestUser_Caris"
            });

            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            A.CallTo(() => _fakeCarisProjectHelper.MarkCarisProjectAsComplete(123, A<int>.Ignored)).MustHaveHappened();

        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_signOff_must_not_run_validation()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";

            _verifyModel.Verifier = TestUser;

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            var childProcessId = 555;

            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                WorkflowInstanceId = 2,
                ProcessId = childProcessId,
                ActivityName = "Verify",
                SerialNumber = "555_456",
                ParentProcessId = ProcessId,
                Status = WorkflowStatus.Started.ToString()

            });

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = ProcessId,
                CorrelationId = Guid.NewGuid()
            });

            await _dbContext.SaveChangesAsync();

            _pageValidationHelper = A.Fake<IPageValidationHelper>();


            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient,
                _fakeCommentsHelper, _fakeAdDirectoryService, _fakeLogger, _pageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);

            await _verifyModel.OnPostDoneAsync(ProcessId, "ConfirmedSignOff");

            // Assert
            A.CallTo(() => _pageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<AdUser>.Ignored,
                                                                                A<bool>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<List<ProductAction>>.Ignored,
                                                                                A.Dummy<bool>(), A.Dummy<string>(), A.Dummy<List<SncAction>>(),
                                                                                A<List<DataImpact>>.Ignored, A<DataImpact>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored))
                                                            .MustNotHaveHappened();

            A.CallTo(() => _pageValidationHelper.CheckVerifyPageForWarnings(A<string>.Ignored,
                                                                                        A<WorkflowInstance>.Ignored,
                                                                                        A<List<DataImpact>>.Ignored,
                                                                                        A<DataImpact>.Ignored,
                                                                                        A<List<string>>.Ignored))
                                                            .MustNotHaveHappened();


            A.CallTo(() => _fakeEventServiceApiClient.PostEvent(
                                                                                nameof(ProgressWorkflowInstanceEvent),
                                                                                A<ProgressWorkflowInstanceEvent>.Ignored))
                                                            .MustHaveHappened();
        }

        [Test]
        public async Task Test_OnPostSaveAsync_That_Setting_Task_To_On_Hold_Creates_A_Row()
        {
            _dbContext.OnHold.Add(new OnHold()
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now.AddDays(-1),
                OnHoldBy = TestUser,
                WorkflowInstanceId = 1
            });

            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _realPortalUserDbService);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);
            _verifyModel.Verifier = TestUser;
            _verifyModel.Team = "HW";
            _verifyModel.RecordSncAction = new List<SncAction>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.IsOnHold = true;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));

            A.CallTo(() => _fakePageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored, A<string>.Ignored,
                    A<List<ProductAction>>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<SncAction>>.Ignored, A<List<DataImpact>>.Ignored, A<DataImpact>.Ignored,
                    A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored))
                .Returns(true);

            await _verifyModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OnHoldTime);
            Assert.AreEqual(onHoldRow.OnHoldBy.UserPrincipalName, TestUser.UserPrincipalName);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_That_Setting_Task_To_Off_Hold_Updates_Existing_Row()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _realPortalUserDbService);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _verifyModel.Verifier = TestUser;
            _verifyModel.Team = "HW";
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.IsOnHold = false;
            _verifyModel.RecordSncAction = new List<SncAction>();

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));

            A.CallTo(() => _fakePageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored, A<string>.Ignored,
                A<List<ProductAction>>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<SncAction>>.Ignored, A<List<DataImpact>>.Ignored, A<DataImpact>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored)).Returns(Task.FromResult(true));

            await _dbContext.OnHold.AddAsync(new OnHold
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now,
                OffHoldBy = TestUser,
                WorkflowInstanceId = 1
            });
            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OffHoldTime);
            Assert.AreEqual(onHoldRow.OffHoldBy.UserPrincipalName, TestUser.UserPrincipalName);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_That_Setting_Task_To_On_Hold_Adds_Comment()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _commentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<AdUser>.Ignored))
                .Returns(true);
            _verifyModel.Verifier = TestUser;
            _verifyModel.Team = "HW";
            _verifyModel.RecordSncAction = new List<SncAction>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.IsOnHold = true;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));

            A.CallTo(() => _fakePageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored, A<string>.Ignored,
                A<List<ProductAction>>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<SncAction>>.Ignored, A<List<DataImpact>>.Ignored, A<DataImpact>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored)).Returns(Task.FromResult(true));

            await _verifyModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comments.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} has been put on hold", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task Test_OnPostSaveAsync_That_Setting_Task_To_Off_Hold_Adds_Comment()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _commentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _verifyModel.Verifier = TestUser;
            _verifyModel.Team = "HW";
            _verifyModel.RecordSncAction = new List<SncAction>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));

            A.CallTo(() => _fakePageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                A<string>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored, A<string>.Ignored,
                A<List<ProductAction>>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<SncAction>>.Ignored, A<List<DataImpact>>.Ignored, A<DataImpact>.Ignored,
                A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<bool>.Ignored)).Returns(Task.FromResult(true));

            _dbContext.OnHold.Add(new OnHold()
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now.AddDays(-1),
                OnHoldBy = TestUser,
                WorkflowInstanceId = 1
            });

            _dbContext.SaveChanges();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comments.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} taken off hold", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task Test_OnPostRejectVerifyAsync_That_Task_With_No_Verifier_Fails_Validation()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser2.UserPrincipalName))
                .Returns(true);

            var row = await _dbContext.DbAssessmentVerifyData.FirstAsync();
            row.Verifier = AdUser.Empty;
            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostRejectVerifyAsync(ProcessId, "Reject");

            Assert.Contains("Operators: You are not assigned as the Verifier of this task. Please assign the task to yourself and click Save", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostRejectVerifyAsync_That_Task_With_Verifier_Fails_Validation_If_CurrentUser_Not_Assigned()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser2.UserPrincipalName))
                .Returns(true);

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = ProcessId,
                CorrelationId = Guid.NewGuid()
            });

            await _verifyModel.OnPostRejectVerifyAsync(ProcessId, "Reject");

            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: {TestUser.DisplayName} is assigned to this task. Please assign the task to yourself and click Save", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostRejectVerifyAsync_Given_CurrentUser_Is_Not_Valid_Then_Returns_Validation_Error_Message()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _realPortalUserDbService);

            var invalidPrincipalName = "THIS-USER-PRINCIPAL-NAME-DOES-NOT-EXIST@example.com";
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("THIS DISPLAY NAME DOES NOT EXIST", invalidPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(invalidPrincipalName))
                .Returns(false);

            var result = (JsonResult)await _verifyModel.OnPostRejectVerifyAsync(ProcessId, "Reject");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, result.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Your user account is not in the correct authorised group. Please contact system administrators",
                _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostRejectVerifyAsync_Given_Valid_Workflow_Then_Workflow_Is_Rejected()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            var correlationId = Guid.NewGuid();
            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = ProcessId,
                CorrelationId = correlationId
            });

            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostRejectVerifyAsync(ProcessId, "Reject");

            var workflowInstance =
                await _dbContext.WorkflowInstance.FirstOrDefaultAsync(wi => wi.ProcessId == ProcessId);

            Assert.IsNotNull(workflowInstance);
            Assert.AreEqual(WorkflowStatus.Updating.ToString(), workflowInstance.Status);
            A.CallTo(() =>
                _fakeEventServiceApiClient.PostEvent(
                    nameof(ProgressWorkflowInstanceEvent),
                    A<ProgressWorkflowInstanceEvent>.That.Matches(p =>
                        p.CorrelationId == correlationId
                        && p.ProcessId == ProcessId
                        && p.FromActivity == WorkflowStage.Verify
                        && p.ToActivity == WorkflowStage.Rejected
                    ))).MustHaveHappened();
            A.CallTo(() => _fakeCommentsHelper.AddComment(
                "Task rejection has been triggered",
                ProcessId,
                workflowInstance.WorkflowInstanceId,
                TestUser.UserPrincipalName)).MustHaveHappened();
        }

        [Test]
        public async Task Test_OnPostDoneAsync_That_Task_With_Verifier_Fails_Validation_If_CurrentUser_Not_Assigned()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser2.DisplayName, TestUser2.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser2.UserPrincipalName))
                .Returns(true);

            await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: {TestUser.DisplayName} is assigned to this task. Please assign the task to yourself and click Save", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_That_Task_Fails_Validation_If_OnHold()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            await _dbContext.OnHold.AddAsync(new OnHold
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now,
                OffHoldBy = TestUser,
                WorkflowInstanceId = 1
            });
            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Task Information: Unable to Sign-off task.Take task off hold before signing-off and click Save.", _verifyModel.ValidationErrorMessages);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Test_OnPostSaveAsync_Given_ProductActionedChangeDetails_Exceeds_Character_Limit_And_ProductActioned_Is_Provided_Then_Validation_Error_Message_Is_Present(bool productActioned)
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.ProductActioned = productActioned;
            _verifyModel.Team = "HW";
            _verifyModel.RecordSncAction = new List<SncAction>();
            //Set ProductActionChangeDetails to 251 characters
            _verifyModel.ProductActionChangeDetails = string.Empty;
            for (int i = 0; i < 25; i++)
            {
                _verifyModel.ProductActionChangeDetails += "0123456789";
            }
            _verifyModel.ProductActionChangeDetails += "0";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Enc Action: Please ensure Enc action change details does not exceed 250 characters", _verifyModel.ValidationErrorMessages);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Test_OnPostSaveAsync_Given_SncActionedChangeDetails_Exceeds_Character_Limit_And_SncActioned_Is_Provided_Then_Validation_Error_Message_Is_Present(bool sncActioned)
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.SncActioned = sncActioned;
            _verifyModel.Team = "HW";
            _verifyModel.RecordSncAction = new List<SncAction>();
            //Set SncActionChangeDetails to 251 characters
            _verifyModel.SncActionChangeDetails = string.Empty;
            for (int i = 0; i < 25; i++)
            {
                _verifyModel.SncActionChangeDetails += "0123456789";
            }
            _verifyModel.SncActionChangeDetails += "0";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Snc Action: Please ensure Snc action change details does not exceed 250 characters", _verifyModel.ValidationErrorMessages);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Test_OnPostDoneAsync_Given_ProductActionedChangeDetails_Exceeds_Character_Limit_And_ProductActioned_Is_Provided_Then_Validation_Error_Message_Is_Present(bool productActioned)
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.ProductActioned = productActioned;
            _verifyModel.Team = "HW";
            _verifyModel.RecordSncAction = new List<SncAction>();
            //Set ProductActionChangeDetails to 251 characters
            _verifyModel.ProductActionChangeDetails = string.Empty;
            for (int i = 0; i < 25; i++)
            {
                _verifyModel.ProductActionChangeDetails += "0123456789";
            }
            _verifyModel.ProductActionChangeDetails += "0";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Enc Action: Please ensure Enc action change details does not exceed 250 characters", _verifyModel.ValidationErrorMessages);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task Test_OnPostDoneAsync_Given_SncActionedChangeDetails_Exceeds_Character_Limit_And_SncActioned_Is_Provided_Then_Validation_Error_Message_Is_Present(bool sncActioned)
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.SncActioned = sncActioned;
            _verifyModel.Team = "HW";
            _verifyModel.RecordSncAction = new List<SncAction>();
            //Set SncActionChangeDetails to 251 characters
            _verifyModel.SncActionChangeDetails = string.Empty;
            for (int i = 0; i < 25; i++)
            {
                _verifyModel.SncActionChangeDetails += "0123456789";
            }
            _verifyModel.SncActionChangeDetails += "0";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Snc Action: Please ensure Snc action change details does not exceed 250 characters", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_where_ProductActioned_ticked_and_no_ProductActionChangeDetails_entered_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "";
            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Enc Action: Please ensure you have entered Enc action change details", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_where_SncActioned_ticked_and_no_SncActionChangeDetails_entered_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.SncActioned = true;
            _verifyModel.SncActionChangeDetails = "";
            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Snc Action: Please ensure you have entered Snc action change details", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_no_ProductActionChangeDetails_entered_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "";
            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Enc Action: Please ensure you have entered Enc action change details", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_SncActioned_ticked_and_no_SncActionChangeDetails_entered_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser.UserPrincipalName))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = AdUser.Empty;
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.SncActioned = true;
            _verifyModel.SncActionChangeDetails = "";
            _verifyModel.Team = "HW";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Snc Action: Please ensure you have entered Snc action change details", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_where_ProductActioned_ticked_and_ProductActionChangeDetails_entered_then_validation_error_message_is_not_present()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "Test change details";

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Enc Action: Please ensure you have entered Enc action change details");
        }

        [Test]
        public async Task Test_OnPostSaveAsync_where_SncActioned_ticked_and_SncActionChangeDetails_entered_then_validation_error_message_is_not_present()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.SncActioned = true;
            _verifyModel.SncActionChangeDetails = "Test change details";

            _verifyModel.RecordSncAction = new List<SncAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Snc Action: Please ensure you have entered Snc action change details");
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_ProductActionChangeDetails_entered_then_validation_error_message_is_not_present()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "Test change details";

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Enc Action: Please ensure you have entered Enc action change details");
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_SncActioned_ticked_and_SncActionChangeDetails_entered_then_validation_error_message_is_not_present()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.SncActioned = true;
            _verifyModel.SncActionChangeDetails = "Test change details";

            _verifyModel.RecordSncAction = new List<SncAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Snc Action: Please ensure you have entered Snc action change details");
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_not_ticked_then_validation_error_messages_are_not_present()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.ProductActioned = false;

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Enc Action: Please ensure you have entered Enc action change details");
            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Enc Action: Please ensure Enc impacted product is fully populated");
            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Enc Action: More than one of the same Enc Impacted Products selected");

        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_SncActioned_not_ticked_then_validation_error_messages_are_not_present()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "HW";
            _verifyModel.Verifier = TestUser;
            _verifyModel.SncActioned = false;

            _verifyModel.RecordSncAction = new List<SncAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Snc Action: Please ensure you have entered Snc action change details");
            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Snc Action: Please ensure Snc impacted product is fully populated");
            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Snc Action: More than one of the same Snc Impacted Products selected");

        }
    }
}
