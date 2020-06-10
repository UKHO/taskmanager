using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Helpers.Auth;
using Common.Messages.Events;
using FakeItEasy;
using HpdDatabase.EF.Models;
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
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class VerifyTests
    {
        private WorkflowDbContext _dbContext;
        private HpdDbContext _hpDbContext;
        private VerifyModel _verifyModel;
        private int ProcessId { get; set; }
        private IWorkflowBusinessLogicService _fakeWorkflowBusinessLogicService;
        private ILogger<VerifyModel> _fakeLogger;
        private IEventServiceApiClient _fakeEventServiceApiClient;
        private IAdDirectoryService _fakeAdDirectoryService;
        private IPortalUserDbService _fakePortalUserDbService;
        private ICommentsHelper _commentsHelper;
        private ICommentsHelper _fakeCommentsHelper;
        private IPageValidationHelper _pageValidationHelper;
        private IPageValidationHelper _fakePageValidationHelper;
        private ICarisProjectHelper _fakeCarisProjectHelper;
        private IOptions<GeneralConfig> _generalConfig;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeWorkflowBusinessLogicService = A.Fake<IWorkflowBusinessLogicService>();
            _fakeEventServiceApiClient = A.Fake<IEventServiceApiClient>();
            _fakeCarisProjectHelper = A.Fake<ICarisProjectHelper>();
            _fakePageValidationHelper = A.Fake<IPageValidationHelper>();
            _generalConfig = A.Fake<IOptions<GeneralConfig>>();

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
                Assessor = "TestUser",
                Verifier = "TestUser"
            });

            _dbContext.ProductAction.Add(new ProductAction
            {
                ProcessId = ProcessId,
                ImpactedProduct = "",
                ProductActionType = new ProductActionType { Name = "Test" },
                Verified = false
            });

            _dbContext.DataImpact.Add(new DataImpact
            {
                ProcessId = ProcessId
            });

            _dbContext.SaveChanges();

            var hpdDbContextOptions = new DbContextOptionsBuilder<HpdDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _hpDbContext = new HpdDbContext(hpdDbContextOptions);

            _commentsHelper = new CommentsHelper(_dbContext);
            _fakeCommentsHelper = A.Fake<ICommentsHelper>();

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakePortalUserDbService = A.Fake<IPortalUserDbService>();

            _fakeLogger = A.Dummy<ILogger<VerifyModel>>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _hpDbContext, _fakeAdDirectoryService, _fakePortalUserDbService);

            _verifyModel = new VerifyModel(_dbContext,
                _fakeWorkflowBusinessLogicService,
                _fakeEventServiceApiClient,
                _fakeCommentsHelper,
                _fakeAdDirectoryService,
                _fakeLogger,
                _pageValidationHelper,
                _fakeCarisProjectHelper,
                _generalConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_entering_an_empty_ion_activityCode_sourceCategory_results_in_validation_error_message_On_Save()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "";
            _verifyModel.ActivityCode = "";
            _verifyModel.SourceCategory = "";

            _verifyModel.Verifier = "TestUser";
            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.Contains($"Task Information: Ion cannot be empty", _verifyModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Activity code cannot be empty", _verifyModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Source category cannot be empty", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_entering_an_empty_Verifier_results_in_validation_error_message_On_Save()
        {
            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = "";
            _verifyModel.DataImpacts = new List<DataImpact>();

            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Verifier cannot be empty", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_entering_invalid_username_for_verifier_results_in_validation_error_message_On_Save()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(false);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";

            _verifyModel.Verifier = "TestUser";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Unable to set Verifier to unknown user {_verifyModel.Verifier}", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_entering_duplicate_hpd_usages_in_dataImpact_results_in_validation_error_message_On_Save()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = "TestUser";
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

            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Data Impact: More than one of the same Usage selected", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_entering_non_existing_impactedProduct_in_productAction_results_in_validation_error_message_On_Save()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _hpDbContext.CarisProducts.Add(new CarisProduct()
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = "TestUser";
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "Some change details";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB5678", ProductActionTypeId = 1}
            };

            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record Product Action: Impacted product GB5678 does not exist", _verifyModel.ValidationErrorMessages);
        }


        [Test]
        public async Task Test_entering_duplicate_impactedProducts_in_productAction_results_in_validation_error_message_On_Save()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _hpDbContext.CarisProducts.Add(new CarisProduct()
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = "TestUser";
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "Some change details";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1234", ProductActionTypeId = 1}
            };

            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record Product Action: More than one of the same Impacted Products selected", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_unverified_productactions_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser", "testuser@foobar.com"));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1235", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = "TestUser";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1}
            };

            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record Product Action: All Product Actions must be verified", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_features_unverified_on_dataimpacts_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser", "testuser@foobar.com"));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1235", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();


            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";

            _verifyModel.Verifier = "TestUser";

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

            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.WarningsDetected, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Data Impact: There are incomplete Features Verified tick boxes.", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_unverified_features_on_empty_dataimpacts_then_no_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser", "testuser@foobar.com"));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1235", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";
            _verifyModel.Verifier = "TestUser";

            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };

            _verifyModel.DataImpacts = new List<DataImpact>();

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreNotEqual((int)VerifyCustomHttpStatusCode.WarningsDetected, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.False(_verifyModel.ValidationErrorMessages.Contains("Data Impact: There are incomplete Features Verified tick boxes."));
        }

        [Test]
        public async Task Test_entering_empty_team_results_in_validation_error_message_On_Save()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "";

            _verifyModel.Verifier = "TestUser";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Task Information: Team cannot be empty", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_has_active_child_tasks_returns_warning_message()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser", "testuser@foobar.com"));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";

            _verifyModel.Verifier = "TestUser";

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
                .Returns(("TestUser", "testuser@foobar.com"));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";

            _verifyModel.Verifier = "TestUser";

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
            string aduser = "TestUser";
            string aduserEmail = "testuser@foobar.com";

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((aduser, aduserEmail));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";

            _verifyModel.Verifier = aduser;

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
                AdUsername = aduser,
                HpdUsername = "TestUser_Caris"
            });

            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            A.CallTo(() => _fakeCarisProjectHelper.MarkCarisProjectAsComplete(123, A<int>.Ignored)).MustHaveHappened();

        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_signOff_must_not_run_validation()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";

            _verifyModel.Verifier = "TestUser";

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
                _fakeCommentsHelper, _fakeAdDirectoryService, _fakeLogger, _pageValidationHelper, _fakeCarisProjectHelper, _generalConfig);

            await _verifyModel.OnPostDoneAsync(ProcessId, "ConfirmedSignOff");

            // Assert
            A.CallTo(() => _pageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<bool>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<List<ProductAction>>.Ignored,
                                                                                A<List<DataImpact>>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<List<string>>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored))
                                                            .MustNotHaveHappened();

            A.CallTo(() => _pageValidationHelper.CheckVerifyPageForWarnings(A<string>.Ignored,
                                                                                        A<WorkflowInstance>.Ignored,
                                                                                        A<List<DataImpact>>.Ignored,
                                                                                        A<List<string>>.Ignored))
                                                            .MustNotHaveHappened();


            A.CallTo(() => _fakeEventServiceApiClient.PostEvent(
                                                                                nameof(ProgressWorkflowInstanceEvent),
                                                                                A<ProgressWorkflowInstanceEvent>.Ignored))
                                                            .MustHaveHappened();
        }

        [Test]
        public async Task Test_That_Setting_Task_To_On_Hold_Creates_A_Row_On_Save()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _verifyModel.Verifier = "TestUser2";
            _verifyModel.Team = "Home Waters";
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.IsOnHold = true;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser2", "testuser2@foobar.com"));

            A.CallTo(() => _fakePageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored, A<List<DataImpact>>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(true);

            await _verifyModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OnHoldTime);
            Assert.AreEqual(onHoldRow.OnHoldUser, "TestUser2");
        }

        [Test]
        public async Task Test_That_Setting_Task_To_Off_Hold_Updates_Existing_Row_On_Save()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _verifyModel.Verifier = "TestUser2";
            _verifyModel.Team = "Home Waters";
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser2", "testuser2@foobar.com"));

            A.CallTo(() => _fakePageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored, A<List<DataImpact>>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult(true));

            await _dbContext.OnHold.AddAsync(new OnHold
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now,
                OffHoldUser = "TestUser2",
                WorkflowInstanceId = 1
            });
            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OffHoldTime);
            Assert.AreEqual(onHoldRow.OffHoldUser, "TestUser2");
        }

        [Test]
        public async Task Test_That_Setting_Task_To_On_Hold_Adds_Comment_On_Save()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _commentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _verifyModel.Verifier = "TestUser2";
            _verifyModel.Team = "Home Waters";
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.IsOnHold = true;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser2", "testuser2@foobar.com"));

            A.CallTo(() => _fakePageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored, A<List<DataImpact>>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult(true));

            await _verifyModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comment.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} has been put on hold", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task Test_That_Setting_Task_To_Off_Hold_Adds_Comment_On_Save()
        {
            _verifyModel = new VerifyModel(_dbContext, _fakeWorkflowBusinessLogicService, _fakeEventServiceApiClient, _commentsHelper, _fakeAdDirectoryService,
                _fakeLogger, _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _verifyModel.Verifier = "TestUser2";
            _verifyModel.Team = "Home Waters";
            _verifyModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser2", "testuser2@foobar.com"));

            A.CallTo(() => _fakePageValidationHelper.CheckVerifyPageForErrors(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored, A<List<DataImpact>>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(Task.FromResult(true));

            _dbContext.OnHold.Add(new OnHold()
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now.AddDays(-1),
                OnHoldUser = "TestUser",
                WorkflowInstanceId = 1
            });

            _dbContext.SaveChanges();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comment.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} taken off hold", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task Test_That_Task_With_No_Verifier_Fails_Validation_On_Reject()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("This User", "thisuser@foobar.com"));

            var row = await _dbContext.DbAssessmentVerifyData.FirstAsync();
            row.Verifier = "";
            await _dbContext.SaveChangesAsync();

            await _verifyModel.OnPostRejectVerifyAsync(ProcessId, "Reject");

            Assert.Contains("Operators: You are not assigned as the Verifier of this task. Please assign the task to yourself and click Save", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_That_Task_With_Verifier_Fails_Validation_If_CurrentUser_Not_Assigned_At_Reject()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser2", "testuser2@foobar.com"));

            await _verifyModel.OnPostRejectVerifyAsync(ProcessId, "Reject");

            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Operators: TestUser is assigned to this task. Please assign the task to yourself and click Save", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_That_Task_With_Verifier_Fails_Validation_If_CurrentUser_Not_Assigned_At_Done()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser2", "testuser2@foobar.com"));

            await _verifyModel.OnPostRejectVerifyAsync(ProcessId, "Done");

            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Operators: TestUser is assigned to this task. Please assign the task to yourself and click Save", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_no_ProductActionChangeDetails_entered_then_validation_error_message_is_present_On_Save()
        {
            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = "";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "";
            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Product Action: Please ensure you have entered product action change details", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_no_ProductActionChangeDetails_entered_then_validation_error_message_is_present_On_Done()
        {
            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Verifier = "";
            _verifyModel.DataImpacts = new List<DataImpact>();
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "";
            _verifyModel.Team = "Home Waters";

            var response = (JsonResult)await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)VerifyCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_verifyModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Product Action: Please ensure you have entered product action change details", _verifyModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_ProductActionChangeDetails_entered_then_validation_error_message_is_not_present_On_Save()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";
            _verifyModel.Verifier = "TestUser";
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "Test change details";

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Product Action: Please ensure you have entered product action change details");
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_ProductActionChangeDetails_entered_then_validation_error_message_is_not_present_On_Done()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";
            _verifyModel.Verifier = "TestUser";
            _verifyModel.ProductActioned = true;
            _verifyModel.ProductActionChangeDetails = "Test change details";

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostDoneAsync(ProcessId, "Done");

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Product Action: Please ensure you have entered product action change details");
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_not_ticked_then_validation_error_messages_are_not_present_On_Done()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _verifyModel.Ion = "Ion";
            _verifyModel.ActivityCode = "ActivityCode";
            _verifyModel.SourceCategory = "SourceCategory";
            _verifyModel.Team = "Home Waters";
            _verifyModel.Verifier = "TestUser";
            _verifyModel.ProductActioned = false;

            _verifyModel.RecordProductAction = new List<ProductAction>();
            _verifyModel.DataImpacts = new List<DataImpact>();

            await _verifyModel.OnPostSaveAsync(ProcessId);

            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Product Action: Please ensure you have entered product action change details");
            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Product Action: Please ensure impacted product is fully populated");
            CollectionAssert.DoesNotContain(_verifyModel.ValidationErrorMessages, "Record Product Action: More than one of the same Impacted Products selected");

        }
    }
}
