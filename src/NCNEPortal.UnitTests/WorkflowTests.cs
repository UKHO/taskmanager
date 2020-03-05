using Common.Helpers;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Configuration;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class WorkflowTests
    {
        private NcneWorkflowDbContext _dbContext;
        private WorkflowModel _workflowModel;
        private ILogger<WorkflowModel> _fakeLogger;
        private IUserIdentityService _fakeUserIdentityService;
        private ICommentsHelper _fakecommentsHelper;
        private ICarisProjectHelper _carisProjectHelper;
        private IMilestoneCalculator _milestoneCalculator;
        private IDirectoryService _fakeDirectoryService;
        private IPageValidationHelper _pageValidationHelper;
        private readonly IOptions<GeneralConfig> _fakeGeneralConfig;
        private int ProcessId { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new NcneWorkflowDbContext(dbContextOptions);
            _fakecommentsHelper = A.Fake<ICommentsHelper>();

            ProcessId = 123;


            _fakeUserIdentityService = A.Fake<IUserIdentityService>();
            _fakeDirectoryService = A.Fake<IDirectoryService>();

            _fakeLogger = A.Dummy<ILogger<WorkflowModel>>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _fakeUserIdentityService, _fakeDirectoryService);


            _workflowModel = new WorkflowModel(_dbContext, _fakeLogger, _fakeUserIdentityService, _fakecommentsHelper, _carisProjectHelper,
                                  _fakeGeneralConfig, _milestoneCalculator, _fakeDirectoryService, _pageValidationHelper);

        }


        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_validating_adoption_charttype_for_repromate_date()
        {
            _workflowModel.ProcessId = 123;
            _workflowModel.RepromatDate = null;
            _workflowModel.ChartType = "Adoption";
            _workflowModel.PublicationDate = null;
            _workflowModel.Compiler = "Stuart";

            await _workflowModel.OnPostSaveAsync(_workflowModel.ProcessId, _workflowModel.ChartType);

            Assert.GreaterOrEqual(_workflowModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_workflowModel.ValidationErrorMessages.Contains("Task Information: Repromat Date cannot be empty"));

        }

        [Test]
        public async Task Test_validating_Primary_charttype_for_publication_date()
        {
            _workflowModel.ProcessId = 123;
            _workflowModel.RepromatDate = null;
            _workflowModel.ChartType = "Primary";
            _workflowModel.PublicationDate = null;
            _workflowModel.Compiler = "Stuart";

            await _workflowModel.OnPostSaveAsync(_workflowModel.ProcessId, _workflowModel.ChartType);

            Assert.GreaterOrEqual(_workflowModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_workflowModel.ValidationErrorMessages.Contains("Task Information: Publication Date cannot be empty"));

        }

        [Test]
        public async Task Test_validating_the_workflow_for_Duration()
        {
            _workflowModel.ProcessId = 123;
            _workflowModel.RepromatDate = DateTime.Now;
            _workflowModel.ChartType = "Primary";
            _workflowModel.PublicationDate = DateTime.Now;
            _workflowModel.Compiler = "Stuart";
            _workflowModel.Dating = 0;

            await _workflowModel.OnPostSaveAsync(_workflowModel.ProcessId, _workflowModel.ChartType);

            Assert.GreaterOrEqual(_workflowModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_workflowModel.ValidationErrorMessages.Contains("Task Information: Duration cannot be empty"));

        }

        [Test]
        public async Task Test_validating_the_workflow_for_Compiler()
        {
            _workflowModel.ProcessId = 123;
            _workflowModel.RepromatDate = DateTime.Now;
            _workflowModel.ChartType = "Primary";
            _workflowModel.PublicationDate = DateTime.Now;
            _workflowModel.Compiler = null;
            _workflowModel.Dating = 0;

            await _workflowModel.OnPostSaveAsync(_workflowModel.ProcessId, _workflowModel.ChartType);

            Assert.GreaterOrEqual(_workflowModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_workflowModel.ValidationErrorMessages.Contains("Task Information: Compiler can not be empty"));

        }

        [Test]
        public async Task Test_validating_the_taskinformation_all_valid()
        {
            _workflowModel.ProcessId = 123;
            _workflowModel.RepromatDate = DateTime.Now;
            _workflowModel.ChartType = "Primary";
            _workflowModel.PublicationDate = DateTime.Now;
            _workflowModel.Compiler = "Stuat";
            _workflowModel.Dating = 1;

            await _workflowModel.OnPostSaveAsync(_workflowModel.ProcessId, _workflowModel.ChartType);

            Assert.GreaterOrEqual(_workflowModel.ValidationErrorMessages.Count, 0);


        }
    }

}
