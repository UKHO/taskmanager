using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class NewTaskTests
    {
        private NcneWorkflowDbContext _dbContext;
        private NewTaskModel _newTaskModel;
        private ILogger<NewTaskModel> _fakeLogger;
        private IUserIdentityService _fakeUserIdentityService;
        private IMilestoneCalculator _milestoneCalculator;
        private IDirectoryService _fakeDirectoryService;
        private IPageValidationHelper _pageValidationHelper;
        private IStageTypeFactory _stageTypeFactory;
        private int ProcessId { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new NcneWorkflowDbContext(dbContextOptions);

            ProcessId = 123;


            _fakeUserIdentityService = A.Fake<IUserIdentityService>();
            _fakeDirectoryService = A.Fake<IDirectoryService>();

            _fakeLogger = A.Dummy<ILogger<NewTaskModel>>();

            _pageValidationHelper = new PageValidationHelper(_dbContext, _fakeUserIdentityService, _fakeDirectoryService);

            _stageTypeFactory = new StageTypeFactory(_dbContext);


            _newTaskModel = new NewTaskModel(_dbContext, _milestoneCalculator, _fakeLogger, _fakeUserIdentityService, _fakeDirectoryService, _stageTypeFactory, _pageValidationHelper);


        }


        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }


        [Test]
        public async Task Test_validating_newtask_for_chart_type()
        {

            _newTaskModel.Compiler = "Pete";

            _newTaskModel.WorkflowType = "NC";

            _newTaskModel.ChartType = null;

            await _newTaskModel.OnPostSaveAsync();

            Assert.GreaterOrEqual(_newTaskModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_newTaskModel.ValidationErrorMessages.Contains("Task Information: Chart Type cannot be empty"));
        }


        [Test]
        public async Task Test_validating_newtask_for_workflow_Type()
        {

            _newTaskModel.Compiler = "Pete";

            _newTaskModel.WorkflowType = null;

            _newTaskModel.ChartType = "Adoption";

            await _newTaskModel.OnPostSaveAsync();

            Assert.GreaterOrEqual(_newTaskModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_newTaskModel.ValidationErrorMessages.Contains("Task Information: Workflow Type cannot be empty"));
        }



        [Test]
        public async Task Test_validating_the_newtask_for_Compiler()
        {
            _newTaskModel.Compiler = null;

            _newTaskModel.WorkflowType = "NE";

            _newTaskModel.ChartType = "Adoption";


            await _newTaskModel.OnPostSaveAsync();

            Assert.GreaterOrEqual(_newTaskModel.ValidationErrorMessages.Count, 1);
            Assert.IsTrue(_newTaskModel.ValidationErrorMessages.Contains("Task Information: Compiler can not be empty"));


        }
    }
}
