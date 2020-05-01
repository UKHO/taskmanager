using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Helpers;
using Common.Helpers.Auth;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Auth;
using Portal.Configuration;
using Portal.Helpers;
using Portal.Pages;
using Portal.ViewModels;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class IndexTests
    {
        private WorkflowDbContext _dbContext;
        private IMapper _mapper;
        private IAdDirectoryService _adDirectoryService;
        private IPortalUserDbService _iPortalUserDbService;
        private ILogger<IndexModel> _logger;
        private IIndexFacade _indexFacade;
        private ICarisProjectHelper _carisProjectHelper;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;
        private IndexModel _indexModel;

        private int ProcessId { get; set; }

        [SetUp]
        public void SetUp()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;
            _dbContext = new WorkflowDbContext(dbContextOptions);
            ProcessId = 123;
            _dbContext.SaveChangesAsync();

            _mapper = A.Fake<IMapper>();
            _adDirectoryService = A.Fake<IAdDirectoryService>();
            _iPortalUserDbService = A.Fake<IPortalUserDbService>();
            _logger = A.Fake<ILogger<IndexModel>>();
            _indexFacade = A.Fake<IIndexFacade>();
            _carisProjectHelper = A.Fake<ICarisProjectHelper>();

            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.TeamsAsCsv = "Team1";

            _indexModel = new IndexModel(_dbContext, _mapper, _adDirectoryService,
                                        _iPortalUserDbService, _logger, _indexFacade,
                                        _carisProjectHelper, _generalConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_OnGet_Leaves_Properties_With_Default_Values_When_EffectiveStartDate_Is_Missing(
            string activityName)
        {
            //Properties are: DmEndDate (null), DaysToDmEndDate (null)
            //                DaysToDmEndDateAmberAlert (false), DaysToDmEndDateRedAlert (false)

            await SetupSupportingTestData(activityName);

            A.CallTo(() => _mapper.Map<List<WorkflowInstance>, List<TaskViewModel>>(null))
                .WithAnyArguments().Returns(new List<TaskViewModel>()
                {
                     new TaskViewModel()
                     {
                         ProcessId = ProcessId,
                         TaskStage = activityName
                     }
                });

            await _indexModel.OnGetAsync();

            var task = _indexModel.Tasks.First(tvm => tvm.ProcessId == ProcessId);
            
            A.CallTo(() => _indexFacade.CalculateDmEndDate(DateTime.MinValue, null, null, null))
                .WithAnyArguments().MustNotHaveHappened();
            A.CallTo(() => _indexFacade.DetermineDaysToDmEndDateAlerts(0))
                .WithAnyArguments().MustNotHaveHappened();
            Assert.AreEqual(null, task.DaysToDmEndDate);
            Assert.AreEqual(null, task.DmEndDate);
            Assert.AreEqual(false, task.DaysToDmEndDateAmberAlert);
            Assert.AreEqual(false, task.DaysToDmEndDateRedAlert);
        }

        private async Task SetupSupportingTestData(string activityName)
        {
            var dbAssessmentReviewData = new DbAssessmentReviewData()
            {
                ProcessId = ProcessId,
                TaskType = "Simple"
            };
            _dbContext.DbAssessmentReviewData.Add(dbAssessmentReviewData);

            var dbAssessmentAssessData = new DbAssessmentAssessData()
            {
                ProcessId = ProcessId,
                TaskType = "Simple"
            };
            _dbContext.DbAssessmentAssessData.Add(dbAssessmentAssessData);

            var dbAssessmentVerifyData = new DbAssessmentVerifyData()
            {
                ProcessId = ProcessId,
                TaskType = "Simple"
            };
            _dbContext.DbAssessmentVerifyData.Add(dbAssessmentVerifyData);

            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = 123,
                ActivityName = activityName,
                StartedAt = new DateTime(2020, 01, 01),
                Status = "Started",
                DbAssessmentReviewData = dbAssessmentReviewData,
                DbAssessmentAssessData = dbAssessmentAssessData,
                DbAssessmentVerifyData = dbAssessmentVerifyData
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = 123,
                ReceiptDate = new DateTime(2020, 05, 01)
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}
