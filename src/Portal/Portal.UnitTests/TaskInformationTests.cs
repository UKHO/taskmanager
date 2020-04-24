using System;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Calculators;
using Portal.Configuration;
using Portal.Helpers;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class TaskInformationTests
    {
        private WorkflowDbContext _dbContext;
        private _TaskInformationModel _taskInformationModel;
        private ITaskDataHelper _taskDataHelper;
        private IOnHoldCalculator _onHoldCalculator;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;
        private IDmEndDateCalculator _dmEndDateCalculator;

        private int ProcessId { get; set; }

        [SetUp]
        public void Setup()
        {
            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();

            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            ProcessId = 123;

            _dbContext.SaveChangesAsync();

            _onHoldCalculator = new OnHoldCalculator(_generalConfig);
            _taskDataHelper = new TaskDataHelper(_dbContext);

            _generalConfig.Value.DmEndDateDaysSimple = 14;
            _generalConfig.Value.DmEndDateDaysLTA = 72;
            _dmEndDateCalculator = new DmEndDateCalculator(_generalConfig);

            var generalConfigOptionsSnapshot = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            var generalConfig = new GeneralConfig { TeamsUnassigned = "Unassigned", TeamsAsCsv = "HW,PR" };
            A.CallTo(() => generalConfigOptionsSnapshot.Value).Returns(generalConfig);


            _taskInformationModel =
                new _TaskInformationModel(_dbContext, _onHoldCalculator, null, _taskDataHelper, generalConfigOptionsSnapshot, _dmEndDateCalculator)
                { ProcessId = ProcessId };
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_reviewdata_is_retrieved_when_calling_onGet_at_Review()
        {
            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                ProcessId = ProcessId,
                ActivityCode = "TestCode",
                Ion = "123",
                SourceCategory = "TestCategory"
            });

            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = 123,
                ActivityName = "Review"
            });
            _dbContext.SaveChanges();

            await _taskInformationModel.OnGetAsync(ProcessId, "Review");

            Assert.AreEqual("TestCode", _taskInformationModel.ActivityCode);
            Assert.AreEqual("123", _taskInformationModel.Ion);
            Assert.AreEqual("TestCategory", _taskInformationModel.SourceCategory);
        }

        [Test]
        public async Task Test_assessdata_is_retrieved_when_calling_onGet_at_Assess()
        {
            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                ProcessId = ProcessId,
                ActivityCode = "TestCode",
                Ion = "123",
                SourceCategory = "TestCategory"
            });

            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = 123,
                ActivityName = "Assess"
            });
            _dbContext.SaveChanges();

            await _taskInformationModel.OnGetAsync(ProcessId, "Assess");

            Assert.AreEqual("TestCode", _taskInformationModel.ActivityCode);
            Assert.AreEqual("123", _taskInformationModel.Ion);
            Assert.AreEqual("TestCategory", _taskInformationModel.SourceCategory);
        }

        [TestCase("Review", "Simple")]
        [TestCase("Assess", "Simple")]
        [TestCase("Verify", "Simple")]
        [TestCase("Review", "LTA")]
        [TestCase("Assess", "LTA")]
        [TestCase("Verify", "LTA")]
        public async Task Test_DmEndDate_is_set_when_calling_onGet(string activityName, string taskType)
        {
            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                ProcessId = ProcessId,
                TaskType = taskType
            });

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                ProcessId = ProcessId,
                TaskType = taskType
            });

            _dbContext.DbAssessmentVerifyData.Add(new DbAssessmentVerifyData()
            {
                ProcessId = ProcessId,
                TaskType = taskType
            });

            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = 123,
                ActivityName = activityName
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = 123,
                EffectiveStartDate = new DateTime(2020, 05, 01)
            });

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            if (taskType == "Simple" || activityName == "Review")
            {
                Assert.AreEqual(new DateTime(2020, 05, 15),
                    _taskInformationModel.DmEndDate);
            }
            else if (taskType == "LTA")
            {
                Assert.AreEqual(new DateTime(2020, 07, 12),
                    _taskInformationModel.DmEndDate);
            }
            
        }
    }
}
