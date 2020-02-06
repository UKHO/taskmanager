using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Portal.Auth;
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
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private IEventServiceApiClient _fakeEventServiceApiClient;
        private IUserIdentityService _fakeUserIdentityService;
        private ICommentsHelper _fakeCommentsHelper;
        private IPageValidationHelper _pageValidationHelper;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakeEventServiceApiClient = A.Fake<IEventServiceApiClient>();

            ProcessId = 123;

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                ProcessId = ProcessId
            });

            _dbContext.SaveChanges();

            var hpdDbContextOptions = new DbContextOptionsBuilder<HpdDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _hpDbContext = new HpdDbContext(hpdDbContextOptions);

            _fakeCommentsHelper = A.Fake<ICommentsHelper>();

            _fakeUserIdentityService = A.Fake<IUserIdentityService>();

            _fakeLogger = A.Dummy<ILogger<AssessModel>>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _hpDbContext);

            _assessModel = new AssessModel(_dbContext, null, _fakeWorkflowServiceApiClient, _fakeEventServiceApiClient, _fakeLogger, _fakeCommentsHelper, _fakeUserIdentityService, _pageValidationHelper);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_entering_an_empty_ion_activityCode_sourceCategory_tasktype_results_in_validation_error_message()
        {
            _assessModel.Ion = "";
            _assessModel.ActivityCode = "";
            _assessModel.SourceCategory = "";
            _assessModel.TaskType = "";

            _assessModel.Verifier = "TestUser";
            _assessModel.DataImpacts = new List<DataImpact>();

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(4, _assessModel.ValidationErrorMessages.Count);
            Assert.Contains($"Task Information: Ion cannot be empty", _assessModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Activity code cannot be empty", _assessModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Source category cannot be empty", _assessModel.ValidationErrorMessages);
            Assert.Contains($"Task Information: Task type cannot be empty", _assessModel.ValidationErrorMessages);
        }

        [Test]
        public async Task Test_entering_an_empty_Verifier_results_in_validation_error_message()
        {
            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";

            _assessModel.Verifier = "";
            _assessModel.DataImpacts = new List<DataImpact>();

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Operators: Verifier cannot be empty", _assessModel.ValidationErrorMessages[0]);
        }
        
        [Test]
        public async Task Test_entering_duplicate_hpd_usages_in_dataImpact_results_in_validation_error_message()
        {
            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";

            _assessModel.Verifier = "TestUser";
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

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Data Impact: More than one of the same Usage selected", _assessModel.ValidationErrorMessages[0]);
        }

        [Test]
        public async Task Test_entering_non_existing_impactedProduct_in_productAction_results_in_validation_error_message()
        {

            _hpDbContext.CarisProducts.Add(new CarisProducts()
                {ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC"});
            await _hpDbContext.SaveChangesAsync();

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";

            _assessModel.Verifier = "TestUser";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.RecordProductAction = new List<ProductAction>
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB5678", ProductActionTypeId = 1}
            };

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Record Product Action: Impacted product GB5678 does not exist", _assessModel.ValidationErrorMessages[0]);
        }


        [Test]
        public async Task Test_entering_duplicate_impactedProducts_in_productAction_results_in_validation_error_message()
        {
            _hpDbContext.CarisProducts.Add(new CarisProducts()
                { ProductName = "GB1234", ProductStatus = "Active", TypeKey = "ENC" });
            await _hpDbContext.SaveChangesAsync();

            _assessModel.Ion = "Ion";
            _assessModel.ActivityCode = "ActivityCode";
            _assessModel.SourceCategory = "SourceCategory";
            _assessModel.TaskType = "TaskType";

            _assessModel.Verifier = "TestUser";
            _assessModel.DataImpacts = new List<DataImpact>();
            _assessModel.RecordProductAction = new List<ProductAction>
            {
                new ProductAction() { ProductActionId = 1, ImpactedProduct = "GB1234", ProductActionTypeId = 1},
                new ProductAction() { ProductActionId = 2, ImpactedProduct = "GB1234", ProductActionTypeId = 1}
            };

            await _assessModel.OnPostDoneAsync(ProcessId, "Save");

            Assert.AreEqual(1, _assessModel.ValidationErrorMessages.Count);
            Assert.AreEqual($"Record Product Action: More than one of the same Impacted Products selected", _assessModel.ValidationErrorMessages[0]);
        }
    }
}
