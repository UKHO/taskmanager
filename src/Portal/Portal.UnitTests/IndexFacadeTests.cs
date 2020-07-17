using System;
using System.Collections.Generic;
using Common.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Calculators;
using Portal.Configuration;
using Portal.Helpers;
using Portal.UnitTests.Helpers;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class IndexFacadeTests
    {
        private IDmEndDateCalculator _dmEndDateCalculator;
        private IOnHoldCalculator _onHoldCalculator;
        private IIndexFacade _indexFacade;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;
        private WorkflowDbContext _dbContext;

        public AdUser TestUser { get; set; }

        [SetUp]
        public void SetUp()
        {
            _dbContext = DatabasesHelpers.GetInMemoryWorkflowDbContext();
            TestUser = AdUserHelper.CreateTestUser(_dbContext);

            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.DmEndDateDaysSimple = 14;

            _dmEndDateCalculator = new DmEndDateCalculator(_generalConfig);
            _onHoldCalculator = new OnHoldCalculator(_generalConfig);

            _indexFacade = new IndexFacade(_dmEndDateCalculator, _onHoldCalculator);
        }

        [Test]
        public void Test_IndexFacade_Returns_Correct_DmEndDate_Including_OnHold_Days()
        {
            var onHoldDays = 3;
            var taskType = "Simple";
            var taskStage = "Review";

            // We'll add 3 on hold days to the DmEndDate
            var onHoldRows = new List<OnHold>
            {
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-onHoldDays),
                    OnHoldBy = TestUser,
                    OffHoldTime = null,
                    OffHoldBy = null,
                }
            };

            var effectiveDate = DateTime.Today;
            var result = _indexFacade.CalculateDmEndDate(effectiveDate, taskType, taskStage, onHoldRows);

            Assert.AreEqual(effectiveDate.AddDays(_generalConfig.Value.DmEndDateDaysSimple).AddDays(onHoldDays), result.dmEndDate);
            Assert.AreEqual(_generalConfig.Value.DmEndDateDaysSimple + onHoldDays, result.daysToDmEndDate);

        }

        [Test]
        public void Test_IndexFacade_Returns_Correct_DmEndDate_When_There_Are_No_OnHold_Days()
        {
            var taskType = "Simple";
            var taskStage = "Review";
            var onHoldRows = new List<OnHold>();
            var effectiveDate = DateTime.Today;


            var result = _indexFacade.CalculateDmEndDate(effectiveDate, taskType, taskStage, onHoldRows);
            Assert.AreEqual(effectiveDate.AddDays(_generalConfig.Value.DmEndDateDaysSimple), result.dmEndDate);
            Assert.AreEqual(_generalConfig.Value.DmEndDateDaysSimple, result.daysToDmEndDate);

        }
    }
}
