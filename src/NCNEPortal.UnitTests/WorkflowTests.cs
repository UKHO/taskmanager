using Common.Helpers;
using Common.Helpers.Auth;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class WorkflowTests
    {
        private NcneWorkflowDbContext _dbContext;
        private WorkflowModel _workflowModel;
        private ILogger<WorkflowModel> _fakeLogger;
        private ICommentsHelper _fakecommentsHelper;
        private ICarisProjectHelper _carisProjectHelper;
        private IMilestoneCalculator _milestoneCalculator;
        private IPageValidationHelper _pageValidationHelper;
        private IStageTypeFactory _stageTypeFactory;
        private readonly IOptions<GeneralConfig> _fakeGeneralConfig;
        private IAdDirectoryService _fakeAdDirectoryService;
        private INcneUserDbService _fakencneUserDbService;
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


            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakencneUserDbService = A.Fake<INcneUserDbService>();

            _fakeLogger = A.Dummy<ILogger<WorkflowModel>>();

            _pageValidationHelper = new PageValidationHelper(_fakencneUserDbService);

            _stageTypeFactory = new StageTypeFactory(_dbContext);

            _workflowModel = new WorkflowModel(_dbContext, _fakeLogger, _fakecommentsHelper, _carisProjectHelper,
                                  _fakeGeneralConfig, _milestoneCalculator, _pageValidationHelper, _fakencneUserDbService, _fakeAdDirectoryService);


            AddStageTypes(_dbContext);

        }


        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_TaskStages_after_Onget_method_of_workflow()
        {

            var taskInfo = _dbContext.TaskInfo.Add(
                new TaskInfo()
                {
                    WorkflowType = "NC",
                    ChartType = "Primary",
                    TaskRole = new TaskRole()
                    { Compiler = "Valid User" },
                    TaskStage = new List<TaskStage>()
                    {
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Specification, Status = "InProgress"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Compile, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1_Rework, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2_Rework, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Forms, Status = "InProgress"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Final_Updating, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Hundred_Percent_Check, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Commit_To_Print, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.CIS, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publication, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publish_Chart, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Clear_Vector, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Retire_Old_Version, Status = "Open"},
                        new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Consider_Withdrawn_Charts, Status = "Open"}
                    }
                });

            await _dbContext.SaveChangesAsync();

            var processid = taskInfo.Entity.ProcessId;

            _workflowModel.OnGetAsync(processid);

            Assert.AreEqual(16, _workflowModel.TaskStages.Count);


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
            Assert.IsTrue(_workflowModel.ValidationErrorMessages.Contains("Task Information: Compiler cannot be empty"));

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


        [Test]
        public void Test_StageType_Factory_For_Adoption_ChartType()
        {
            var stagetypes = _stageTypeFactory.GetTaskStages("Adoption");

            var Sdra = stagetypes.Where(t => t.Name == "With SDRA");

            Assert.AreEqual(stagetypes.Count, 18);
            Assert.AreEqual(Sdra.Count(), 1);
        }


        [Test]
        public void Test_StageType_Factory_For_Primary_ChartType()
        {
            var stagetypes = _stageTypeFactory.GetTaskStages("Primary");

            var Sdra = stagetypes.Where(t => t.Name == "With SDRA");

            Assert.AreEqual(stagetypes.Count, 16);
            Assert.AreEqual(Sdra.Count(), 0);
        }

        private void AddStageTypes(NcneWorkflowDbContext dbContext)
        {
            if (!File.Exists(@"Data\TaskStageType.json")) throw new FileNotFoundException(@"Data\TaskStageType.json");

            var jsonString = File.ReadAllText(@"Data\TaskStageType.json");
            var stageType = JsonConvert.DeserializeObject<IEnumerable<TaskStageType>>(jsonString);

            dbContext.TaskStageType.AddRange(stageType);

            _dbContext.SaveChanges();
        }

    }

}
