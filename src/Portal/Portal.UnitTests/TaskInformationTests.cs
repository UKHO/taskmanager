﻿using System;
using System.Linq;
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
        private IDmEndDateCalculator _dmEndDateCalculator;

        private int ProcessId { get; set; }

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            ProcessId = 123;

            _dbContext.SaveChangesAsync();

            _taskDataHelper = new TaskDataHelper(_dbContext);

            var generalConfigOptionsSnapshot = A.Fake<IOptionsSnapshot<GeneralConfig>>();

            var generalConfig = new GeneralConfig { TeamsUnassigned = "Unassigned", TeamsAsCsv = "Home Waters,Primary Charting", ExternalEndDateDays = 20, DmEndDateDaysSimple = 14, DmEndDateDaysLTA = 72 };
            A.CallTo(() => generalConfigOptionsSnapshot.Value).Returns(generalConfig);

            _dmEndDateCalculator = new DmEndDateCalculator(generalConfigOptionsSnapshot);

            _onHoldCalculator = new OnHoldCalculator(generalConfigOptionsSnapshot);

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
            await SetupSupportingTestData(activityName);

            _dbContext.DbAssessmentReviewData.First(ad => ad.ProcessId == ProcessId).TaskType = taskType;
            _dbContext.DbAssessmentAssessData.First(ad => ad.ProcessId == ProcessId).TaskType = taskType;
            _dbContext.DbAssessmentVerifyData.First(ad => ad.ProcessId == ProcessId).TaskType = taskType;

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).EffectiveStartDate =
                new DateTime(2020, 05, 01);

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

        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_ExternalEndDate_is_set_when_calling_onGet(string activityName)
        {
            await SetupSupportingTestData(activityName);

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).EffectiveStartDate =
                new DateTime(2020, 05, 01);

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).ReceiptDate = new DateTime(2020, 05, 01);

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            Assert.AreEqual(new DateTime(2020, 05, 21),
                    _taskInformationModel.ExternalEndDate);
        }

        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_DmReceiptDate_is_set_when_calling_onGet(string activityName)
        {
            await SetupSupportingTestData(activityName);

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).EffectiveStartDate =
                new DateTime(2020, 05, 01);

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).ReceiptDate = new DateTime(2020, 05, 01);

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            Assert.AreEqual(new DateTime(2020, 01, 01),
                _taskInformationModel.DmReceiptDate);
        }

        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_EffectiveReceiptDate_is_set_when_calling_onGet(string activityName)
        {
            await SetupSupportingTestData(activityName);

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).EffectiveStartDate =
                new DateTime(2020, 05, 01);

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).ReceiptDate = new DateTime(2020, 05, 01);

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            Assert.AreEqual(new DateTime(2020, 05, 01),
                _taskInformationModel.EffectiveReceiptDate);
        }

        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_EffectiveReceiptDate_is_set_to_NA_when_calling_onGet_with_no_EffectiveStartDate(string activityName)
        {
            await SetupSupportingTestData(activityName);

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).ReceiptDate = new DateTime(2020, 05, 01);

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            Assert.AreEqual(null,
                _taskInformationModel.EffectiveReceiptDate);
        }

        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_DmEndDate_is_set_to_NA_when_calling_onGet_with_no_EffectiveStartDate(string activityName)
        {
            await SetupSupportingTestData(activityName);

            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).ReceiptDate = new DateTime(2020, 05, 01);

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            Assert.AreEqual(null,
                _taskInformationModel.DmEndDate);
        }


        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_DmEndDate_is_set_to_NA_when_calling_onGet_with_no_TaskData(string activityName)
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = 123,
                ActivityName = activityName,
                StartedAt = new DateTime(2020, 01, 01)
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = 123,
                EffectiveStartDate = new DateTime(2020, 05, 01),
                ReceiptDate = new DateTime(2020, 05, 01)
            });

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            Assert.AreEqual(null,
                _taskInformationModel.DmEndDate);
        }


        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_DmEndDate_is_set_to_NA_when_calling_onGet_with_no_TaskData_and_no_EffectiveStartDate(string activityName)
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = 123,
                ActivityName = activityName,
                StartedAt = new DateTime(2020, 01, 01)
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = 123,
                ReceiptDate = new DateTime(2020, 05, 01)
            });

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            Assert.AreEqual(null,
                _taskInformationModel.DmEndDate);
        }

        [TestCase("Review")]
        [TestCase("Assess")]
        [TestCase("Verify")]
        public async Task Test_ExternalEndDate_is_set_to_NA_when_calling_onGet_with_no_EffectiveStartDate(string activityName)
        {
            await SetupSupportingTestData(activityName);
            
            _dbContext.AssessmentData.First(ad => ad.ProcessId == ProcessId).ReceiptDate = new DateTime(2020, 05, 01);

            await _dbContext.SaveChangesAsync();

            await _taskInformationModel.OnGetAsync(ProcessId, activityName);

            Assert.AreEqual(null,
                _taskInformationModel.ExternalEndDate);
        }

        private async Task SetupSupportingTestData(string activityName)
        {
            _dbContext.DbAssessmentReviewData.Add(new DbAssessmentReviewData()
            {
                ProcessId = ProcessId,
                TaskType = "Simple"
            });

            _dbContext.DbAssessmentAssessData.Add(new DbAssessmentAssessData()
            {
                ProcessId = ProcessId,
                TaskType = "Simple"
            });

            _dbContext.DbAssessmentVerifyData.Add(new DbAssessmentVerifyData()
            {
                ProcessId = ProcessId,
                TaskType = "Simple"
            });

            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = 123,
                ActivityName = activityName,
                StartedAt = new DateTime(2020, 01, 01)
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = 123
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}
