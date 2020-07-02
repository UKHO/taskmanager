using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using Portal.Configuration;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class AssessTests
    {
        private WorkflowDbContext _dbContext;
        private HpdDbContext _hpDbContext;
        private AssessModel _assessModel;
        private int ProcessId { get; set; }
        private ILogger<AssessModel> _fakeLogger;
        private IEventServiceApiClient _fakeEventServiceApiClient;
        private IAdDirectoryService _fakeAdDirectoryService;
        private IPortalUserDbService _fakePortalUserDbService;
        private ICommentsHelper _commentsHelper;
        private ICommentsHelper _fakeDbAssessmentCommentsHelper;
        private IPageValidationHelper _pageValidationHelper;
        private IPageValidationHelper _fakePageValidationHelper;
        private ICarisProjectHelper _fakeCarisProjectHelper;
        private IOptions<GeneralConfig> _generalConfig;

        public AdUser TestUser
        {
            get
            {
                var user = AdUser.Unknown;

                user = _dbContext.AdUsers.SingleOrDefault(u =>
                    u.UserPrincipalName.Equals("test@email.com", StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    user = new AdUser
                    {
                        DisplayName = "Test User",
                        UserPrincipalName = "test@email.com"
                    };
                    _dbContext.SaveChanges();
                }

                return user;
            }
        }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeEventServiceApiClient = A.Fake<IEventServiceApiClient>();
            _fakeCarisProjectHelper = A.Fake<ICarisProjectHelper>();
            _generalConfig = A.Fake<IOptions<GeneralConfig>>();

            ProcessId = 123;

            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                AssessmentData = new AssessmentData(),
                ProcessId = ProcessId,
                ActivityName = "",
                SerialNumber = "123_sn"
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = ProcessId
            });

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                ProcessId = ProcessId,
                Assessor = TestUser
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

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus
            {
                ProcessId = ProcessId,
                CorrelationId = Guid.NewGuid()
            });

            _dbContext.SaveChanges();

            var hpdDbContextOptions = new DbContextOptionsBuilder<HpdDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _hpDbContext = new HpdDbContext(hpdDbContextOptions);

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakePortalUserDbService = A.Fake<IPortalUserDbService>();

            _commentsHelper = new CommentsHelper(_dbContext, _fakePortalUserDbService);
            _fakeDbAssessmentCommentsHelper = A.Fake<ICommentsHelper>();

            _fakeLogger = A.Dummy<ILogger<AssessModel>>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _hpDbContext, _fakeAdDirectoryService, _fakePortalUserDbService);
            _fakePageValidationHelper = A.Fake<IPageValidationHelper>();

            _assessModel = new AssessModel(_dbContext, _fakeEventServiceApiClient, _fakeLogger, _fakeDbAssessmentCommentsHelper, _fakeAdDirectoryService, _pageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_entering_an_empty_ion_activityCode_sourceCategory_tasktype_team_assessor_results_in_validation_error_message()
        {
            _hpDbContext.CarisProducts.Add(new CarisProduct()
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "";
            _assessModel.ActivityCode = "";
            _assessModel.SourceCategory = "";
            _assessModel.TaskType = "";
            _assessModel.Team = "";

            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.DataImpacts = new List<DataImpact>();

            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };

            await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 5);
            Assert.Contains($"Task Information: Ion cannot be empty", _assessModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Activity code cannot be empty", _assessModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Source category cannot be empty", _assessModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Task type cannot be empty", _assessModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Team cannot be empty", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_entering_an_empty_Verifier_results_in_validation_error_message()
        {
            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";
            _assessModel.Team = "HW";
            _assessModel.Assessor = TestUser;

            _assessModel.Verifier = AdUser.Empty;
            _assessModel.DataImpacts = new List<DataImpact>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser))
                .Returns(true);


            await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Verifier cannot be empty", _assessModel.ValidationErrorMessages);

            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_entering_an_empty_assessor_results_in_validation_error_message()
        {
            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";
            _assessModel.Team = "HW";
            _assessModel.Verifier = TestUser;

            _assessModel.Assessor = AdUser.Empty;
            _assessModel.DataImpacts = new List<DataImpact>();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser))
                .Returns(true);

            await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Assessor cannot be empty", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_entering_duplicate_hpd_usages_in_dataImpact_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";
            _assessModel.Team = "HW";

            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;

            var hpdUsage = new HpdUsage()
            {
                HpdUsageId = 1,
                Name = "HpdUsageName"
            };
            _assessModel.DataImpacts = new List<DataImpact>()
            {
                new DataImpact() { DataImpactId = 1, HpdUsageId = 1, HpdUsage = hpdUsage, ProcessId = 123},
                new DataImpact() {DataImpactId = 2, HpdUsageId = 1, HpdUsage = hpdUsage, ProcessId = 123}
            };

            await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Data Impact: More than one of the same Usage selected", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_entering_non_existing_impactedProduct_in_productAction_results_in_validation_error_message()
        {

            _hpDbContext.CarisProducts.Add(new CarisProduct()
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();


            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";
            _assessModel.Team = "HW";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.ProductActioned = true;
            _assessModel.ProductActionChangeDetails = "Some change details";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.RecordProductAction = new List<ProductAction>
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB5678", ProductActionTypeId = 1}
            };


            await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record Product Action: Impacted product GB5678 does not exist", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }


        [Test]
        public async Task Test_entering_duplicate_impactedProducts_in_productAction_results_in_validation_error_message()
        {
            _hpDbContext.CarisProducts.Add(new CarisProduct()
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";
            _assessModel.Team = "HW";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.ProductActioned = true;
            _assessModel.ProductActionChangeDetails = "Some change details";
            _assessModel.RecordProductAction = new List<ProductAction>
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1234", ProductActionTypeId = 1}
            };

            await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Record Product Action: More than one of the same Impacted Products selected", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_entering_invalid_username_for_assessor_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(AdUser.Unknown))
                .Returns(false);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";
            _assessModel.Team = "HW";
            _assessModel.Assessor = AdUser.Unknown;
            _assessModel.Verifier = TestUser;

            await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Unable to set Assessor to unknown user {_assessModel.Assessor}", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_entering_invalid_username_for_verifier_results_in_validation_error_message()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(TestUser))
                .Returns(true);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(AdUser.Unknown))
                .Returns(false);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";
            _assessModel.Team = "HW";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = AdUser.Unknown;

            await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains($"Operators: Unable to set Verifier to unknown user {_assessModel.Verifier}", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_That_Task_With_No_Assessor_Fails_Validation_On_Done()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
               .Returns(("This User", "thisuser@foobar.com"));

            var row = await _dbContext.DbAssessmentAssessData.FirstAsync();
            row.Assessor = null;
            await _dbContext.SaveChangesAsync();

            await _assessModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.Contains("Operators: You are not assigned as the Assessor of this task. Please assign the task to yourself and click Save", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_That_Task_With_Assessor_Fails_Validation_If_CurrentUser_Not_Assigned_At_Done()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));

            await _assessModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Operators: TestUser is assigned to this task. Please assign the task to yourself and click Save", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_OnPostSaveAsync_Given_StsDataUsage_Has_Invalid_HpdUsageId_Then_No_Record_Is_Saved()
        {
            _assessModel = new AssessModel(_dbContext, _fakeEventServiceApiClient, _fakeLogger, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _assessModel.Assessor = "TestUser2";
            _assessModel.Team = "HW";
            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _assessModel.IsOnHold = false;
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.StsDataImpact = new DataImpact() { HpdUsageId = 0 };

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser2", "testuser2@foobar.com"));
            A.CallTo(() => _fakePageValidationHelper.CheckAssessPageForErrors("Save", A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored,
                    A<List<DataImpact>>.Ignored, A<DataImpact>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(true);

            var response = (StatusCodeResult) await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode);

            var stsDataImpact = await _dbContext.DataImpact.SingleOrDefaultAsync(di => di.ProcessId == ProcessId && di.StsUsage);
            
            Assert.IsNull(stsDataImpact);
        }

        [Test]
        public async Task Test_OnPostSaveAsync_Given_StsDataUsage_Has_Valid_HpdUsageId_Then_Record_Is_Saved()
        {
            _assessModel = new AssessModel(_dbContext, _fakeEventServiceApiClient, _fakeLogger, _fakeCommentsHelper, _fakeAdDirectoryService,
                _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _assessModel.Assessor = "TestUser2";
            _assessModel.Team = "HW";
            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _assessModel.IsOnHold = false;
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.StsDataImpact = new DataImpact()
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

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser2", "testuser2@foobar.com"));
            A.CallTo(() => _fakePageValidationHelper.CheckAssessPageForErrors("Save", A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored,
                    A<List<DataImpact>>.Ignored, A<DataImpact>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(true);

            var response = (StatusCodeResult) await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)HttpStatusCode.OK, response.StatusCode);

            var stsDataImpact = await _dbContext.DataImpact.SingleOrDefaultAsync(di => di.ProcessId == ProcessId && di.StsUsage);
            
            Assert.IsNotNull(stsDataImpact);

            Assert.AreEqual(ProcessId, stsDataImpact.ProcessId);
            Assert.AreEqual(_assessModel.StsDataImpact.HpdUsageId, stsDataImpact.HpdUsageId);
            Assert.AreEqual(_assessModel.StsDataImpact.Comments, stsDataImpact.Comments);
            Assert.AreEqual(_assessModel.StsDataImpact.FeaturesVerified, stsDataImpact.FeaturesVerified);

            Assert.IsFalse(stsDataImpact.FeaturesSubmitted);
            Assert.IsFalse(stsDataImpact.Edited);
        }

        [Test]
        public async Task Test_That_Setting_Task_To_On_Hold_Creates_A_Row_On_Save()
        {
            _assessModel = new AssessModel(_dbContext, _fakeEventServiceApiClient, _fakeLogger, _fakeDbAssessmentCommentsHelper, _fakeAdDirectoryService,
                _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _assessModel.Assessor = TestUser;
            _assessModel.Team = "HW";
            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.IsOnHold = true;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePageValidationHelper.CheckAssessPageForErrors("Save", A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored,
                    A<List<DataImpact>>.Ignored,  A< DataImpact>.Ignored,A<string>.Ignored, A<AdUser>.Ignored, A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored))
                .Returns(true);

            await _assessModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OnHoldTime);
            Assert.AreEqual(onHoldRow.OnHoldBy, TestUser);
        }

        [Test]
        public async Task Test_That_Setting_Task_To_Off_Hold_Updates_Existing_Row_On_Save()
        {
            _assessModel = new AssessModel(_dbContext, _fakeEventServiceApiClient, _fakeLogger, _fakeDbAssessmentCommentsHelper, _fakeAdDirectoryService,
                _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _assessModel.Assessor = TestUser;
            _assessModel.Team = "HW";
            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePageValidationHelper.CheckAssessPageForErrors("Save", A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored,
                    A<List<DataImpact>>.Ignored,  A< DataImpact>.Ignored,A<string>.Ignored, A<AdUser>.Ignored, A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored))
                .Returns(true);

            await _dbContext.OnHold.AddAsync(new OnHold
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now,
                OffHoldBy = TestUser,
                WorkflowInstanceId = 1
            });
            await _dbContext.SaveChangesAsync();

            await _assessModel.OnPostSaveAsync(ProcessId);

            var onHoldRow = await _dbContext.OnHold.FirstAsync(o => o.ProcessId == ProcessId);

            Assert.NotNull(onHoldRow);
            Assert.NotNull(onHoldRow.OffHoldTime);
            Assert.AreEqual(onHoldRow.OffHoldBy, TestUser);
        }

        [Test]
        public async Task Test_That_Setting_Task_To_On_Hold_Adds_Comment_On_Save()
        {
            _assessModel = new AssessModel(_dbContext, _fakeEventServiceApiClient, _fakeLogger, _commentsHelper, _fakeAdDirectoryService,
                _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _assessModel.Assessor = TestUser;
            _assessModel.Team = "HW";
            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.IsOnHold = true;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePageValidationHelper.CheckAssessPageForErrors("Save", A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored,
                    A<List<DataImpact>>.Ignored,  A< DataImpact>.Ignored, A<string>.Ignored, A<AdUser>.Ignored, A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored))
                .Returns(true);

            await _assessModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comments.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} has been put on hold", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public async Task Test_That_Setting_Task_To_Off_Hold_Adds_Comment_On_Save()
        {
            _assessModel = new AssessModel(_dbContext, _fakeEventServiceApiClient, _fakeLogger, _commentsHelper, _fakeAdDirectoryService,
                _fakePageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);

            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);
            _assessModel.Assessor = TestUser;
            _assessModel.Team = "HW";
            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() {ImpactedProduct = "GB1234", ProcessId = 123, ProductActionTypeId = 1}
            };
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.IsOnHold = false;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePageValidationHelper.CheckAssessPageForErrors("Save", A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                    A<string>.Ignored, A<bool>.Ignored, A<string>.Ignored, A<List<ProductAction>>.Ignored,
                    A<List<DataImpact>>.Ignored, A< DataImpact>.Ignored,  A<string>.Ignored, A<AdUser>.Ignored, A<AdUser>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<AdUser>.Ignored))
                .Returns(true);

            _dbContext.OnHold.Add(new OnHold()
            {
                ProcessId = ProcessId,
                OnHoldTime = DateTime.Now.AddDays(-1),
                OnHoldBy = TestUser,
                WorkflowInstanceId = 1
            });

            _dbContext.SaveChanges();

            await _assessModel.OnPostSaveAsync(ProcessId);

            var comments = await _dbContext.Comments.Where(c => c.ProcessId == ProcessId).ToListAsync();

            Assert.GreaterOrEqual(comments.Count, 1);
            Assert.IsTrue(comments.Any(c =>
                c.Text.Contains($"Task {ProcessId} taken off hold", StringComparison.OrdinalIgnoreCase)));
        }


        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_stsdataimpact_usage_not_selected_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns(("TestUser", "testuser@foobar.com"));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = "TestUser2";
            _assessModel.Verifier = "TestUser2";
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";

            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true}
            };

            var hpdUsage1 = new HpdUsage()
            {
                HpdUsageId = 1,
                Name = "HpdUsageName1"
            };
            _assessModel.DataImpacts = new List<DataImpact>()
            {
                new DataImpact() { DataImpactId = 1, HpdUsageId = 1, HpdUsage = hpdUsage1, FeaturesSubmitted = true, ProcessId = 123}
            };

            _assessModel.Team = "HW";

            var response = (JsonResult)await _assessModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)AssessCustomHttpStatusCode.WarningsDetected, response.StatusCode);
            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Data Impact: STS Usage has not been selected, are you sure you want to continue?", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_features_unsubmitted_on_dataimpacts_then_validation_error_message_is_present()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1235", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";

            _assessModel.RecordProductAction = new List<ProductAction>()
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
            _assessModel.DataImpacts = new List<DataImpact>()
            {
                new DataImpact() { DataImpactId = 1, HpdUsageId = 1, HpdUsage = hpdUsage1, ProcessId = 123},
                new DataImpact() {DataImpactId = 2, HpdUsageId = 2, HpdUsage = hpdUsage2, ProcessId = 123}
            };

            _assessModel.Team = "HW";

            var response = (JsonResult)await _assessModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)AssessCustomHttpStatusCode.WarningsDetected, response.StatusCode);
            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Data Impact: There are incomplete Features Submitted tick boxes.", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_done_and_unsubmitted_features_on_empty_dataimpacts_then_no_validation_error_message_is_present()
        {
            var correlationId = Guid.NewGuid();

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            _hpDbContext.CarisProducts.Add(new CarisProduct
            { ProductName = "GB1235", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();


            var primaryDocumentStatus =
                await _dbContext.PrimaryDocumentStatus.FirstOrDefaultAsync(pds => pds.ProcessId == ProcessId);

            primaryDocumentStatus.CorrelationId = correlationId;

            await _dbContext.SaveChangesAsync();

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";

            _assessModel.RecordProductAction = new List<ProductAction>()
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1, Verified = true},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1235", ProductActionTypeId = 1, Verified = true}
            };

            _assessModel.DataImpacts = new List<DataImpact>();

            _assessModel.StsDataImpact = new DataImpact() { HpdUsageId = 1 };


            var response = await _assessModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)HttpStatusCode.OK, ((StatusCodeResult)response).StatusCode);
            Assert.AreEqual(0, _assessModel.ValidationErrorMessages.Count);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(
                                            nameof(ProgressWorkflowInstanceEvent),
                                            A<ProgressWorkflowInstanceEvent>.That.Matches(p =>
                                                                                                        p.CorrelationId == correlationId
                                                                                                        && p.ProcessId == ProcessId
                                                                                                        && p.FromActivity == WorkflowStage.Assess
                                                                                                        && p.ToActivity == WorkflowStage.Verify
                    ))).MustHaveHappened();
        }

        [Test]
        public async Task Test_OnPostDoneAsync_given_action_confirmedDone_must_not_run_validation()
        {
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Team = "HW";

            _assessModel.Verifier = TestUser;

            _assessModel.RecordProductAction = new List<ProductAction>();
            _assessModel.DataImpacts = new List<DataImpact>();

            var childProcessId = 555;

            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                WorkflowInstanceId = 2,
                ProcessId = childProcessId,
                ActivityName = "Assess",
                SerialNumber = "555_456",
                ParentProcessId = ProcessId,
                Status = WorkflowStatus.Started.ToString()

            });

            await _dbContext.SaveChangesAsync();

            _pageValidationHelper = A.Fake<IPageValidationHelper>();

            _assessModel = new AssessModel(_dbContext, _fakeEventServiceApiClient, _fakeLogger, _fakeDbAssessmentCommentsHelper, _fakeAdDirectoryService, _pageValidationHelper, _fakeCarisProjectHelper, _generalConfig, _fakePortalUserDbService);


            await _assessModel.OnPostDoneAsync(ProcessId, "ConfirmedDone");

            // Assert
            A.CallTo(() => _pageValidationHelper.CheckAssessPageForErrors(A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<bool>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<List<ProductAction>>.Ignored,
                                                                                A<List<DataImpact>>.Ignored,
                                                                                A<DataImpact>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<AdUser>.Ignored,
                                                                                A<AdUser>.Ignored,
                                                                                A<List<string>>.Ignored,
                                                                                A<string>.Ignored,
                                                                                A<AdUser>.Ignored))
                                                            .MustNotHaveHappened();

            A.CallTo(() => _pageValidationHelper.CheckAssessPageForWarnings(A<string>.Ignored,
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
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_no_ProductActionChangeDetails_entered_then_validation_error_message_is_present_On_Done()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";
            _assessModel.ProductActioned = true;
            _assessModel.ProductActionChangeDetails = "";

            var response = (JsonResult)await _assessModel.OnPostDoneAsync(ProcessId, "Done");

            Assert.AreEqual((int)AssessCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Product Action: Please ensure you have entered product action change details", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_no_ProductActionChangeDetails_entered_then_validation_error_message_is_present_On_Save()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";
            _assessModel.ProductActioned = true;
            _assessModel.ProductActionChangeDetails = "";

            var response = (JsonResult)await _assessModel.OnPostSaveAsync(ProcessId);

            Assert.AreEqual((int)AssessCustomHttpStatusCode.FailedValidation, response.StatusCode);
            Assert.GreaterOrEqual(_assessModel.ValidationErrorMessages.Count, 1);
            Assert.Contains("Record Product Action: Please ensure you have entered product action change details", _assessModel.ValidationErrorMessages);
            A.CallTo(() =>
                    _fakeEventServiceApiClient.PostEvent(A<string>.Ignored, A<ProgressWorkflowInstanceEvent>.Ignored))
                .WithAnyArguments().MustNotHaveHappened();
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_ProductActionChangeDetails_entered_then_validation_error_message_is_not_present_On_Done()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.ProductActioned = true;
            _assessModel.ProductActionChangeDetails = "Test change details";

            await _assessModel.OnPostDoneAsync(ProcessId, "Done");

            CollectionAssert.DoesNotContain(_assessModel.ValidationErrorMessages, "Record Product Action: Please ensure you have entered product action change details");
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_ticked_and_ProductActionChangeDetails_entered_then_validation_error_message_is_not_present_On_Save()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.ProductActioned = true;
            _assessModel.ProductActionChangeDetails = "Test change details";

            await _assessModel.OnPostSaveAsync(ProcessId);

            CollectionAssert.DoesNotContain(_assessModel.ValidationErrorMessages, "Record Product Action: Please ensure you have entered product action change details");
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_not_ticked_then_validation_error_messages_are_not_present_on_Done()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.ProductActioned = false;

            await _assessModel.OnPostDoneAsync(ProcessId, "Done");

            CollectionAssert.DoesNotContain(_assessModel.ValidationErrorMessages, "Record Product Action: Please ensure you have entered product action change details");
            CollectionAssert.DoesNotContain(_assessModel.ValidationErrorMessages, "Record Product Action: Please ensure impacted product is fully populated");
            CollectionAssert.DoesNotContain(_assessModel.ValidationErrorMessages, "Record Product Action: More than one of the same Impacted Products selected");
        }

        [Test]
        public async Task Test_OnPostDoneAsync_where_ProductActioned_not_ticked_then_validation_error_messages_are_not_present_on_Save()
        {
            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((TestUser.DisplayName, TestUser.UserPrincipalName));
            A.CallTo(() => _fakePortalUserDbService.ValidateUserAsync(A<string>.Ignored))
                .Returns(true);

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.Assessor = TestUser;
            _assessModel.Verifier = TestUser;
            _assessModel.Team = "HW";
            _assessModel.TaskType = "TaskType";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.ProductActioned = false;

            await _assessModel.OnPostSaveAsync(ProcessId);

            CollectionAssert.DoesNotContain(_assessModel.ValidationErrorMessages, "Record Product Action: Please ensure you have entered product action change details");
            CollectionAssert.DoesNotContain(_assessModel.ValidationErrorMessages, "Record Product Action: Please ensure impacted product is fully populated");
            CollectionAssert.DoesNotContain(_assessModel.ValidationErrorMessages, "Record Product Action: More than one of the same Impacted Products selected");
        }



        [TestCase("Done")]
        [TestCase("ConfirmedDone")]
        public async Task Test_OnPostDoneAsync_When_All_Steps_Were_Successful_Then_Status_Is_Updating_And_Progress_Event_Is_Fired_And_Comment_Is_Added(string action)
        {
            var correlationId = Guid.NewGuid();
            var userFullName = TestUser.DisplayName;
            var userEmail = TestUser.UserPrincipalName;

            A.CallTo(() => _fakeAdDirectoryService.GetUserDetails(A<ClaimsPrincipal>.Ignored))
                .Returns((userFullName, userEmail));

            A.CallTo(() => _fakePageValidationHelper.CheckAssessPageForErrors(
                null,
                null,
                null,
                null,
                null,
                A<bool>.Ignored,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null)).WithAnyArguments().Returns(true);

            A.CallTo(() => _fakePageValidationHelper.CheckAssessPageForWarnings(
                null,
                null,
                null,
                null)).WithAnyArguments().Returns(false);

            var primaryDocumentStatus =
                await _dbContext.PrimaryDocumentStatus.FirstOrDefaultAsync(pds => pds.ProcessId == ProcessId);

            primaryDocumentStatus.CorrelationId = correlationId;

            await _dbContext.SaveChangesAsync();

            _assessModel = new AssessModel(
                            _dbContext,
                            _fakeEventServiceApiClient,
                            _fakeLogger,
                            _fakeDbAssessmentCommentsHelper,
                            _fakeAdDirectoryService,
                            _fakePageValidationHelper,
                            _fakeCarisProjectHelper,
                            _generalConfig, _fakePortalUserDbService);

            _assessModel.DataImpacts = new List<DataImpact>();

            await _assessModel.OnPostDoneAsync(ProcessId, action);

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
                        && p.FromActivity == WorkflowStage.Assess
                        && p.ToActivity == WorkflowStage.Verify
                    ))).MustHaveHappened();
            A.CallTo(() => _fakeDbAssessmentCommentsHelper.AddComment(
                "Task progression from Assess to Verify has been triggered",
                ProcessId,
                workflowInstance.WorkflowInstanceId,
                userEmail)).MustHaveHappened();
        }
    }
}
