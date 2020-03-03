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

            var generalConfigOptionsSnapshot = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            var generalConfig = new GeneralConfig { TeamsUnassigned = "Unassigned", TeamsAsCsv = "HW,PR" };
            A.CallTo(() => generalConfigOptionsSnapshot.Value).Returns(generalConfig);


            _taskInformationModel =
                new _TaskInformationModel(_dbContext, _onHoldCalculator, null, null, _taskDataHelper, generalConfigOptionsSnapshot)
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
    }
}
