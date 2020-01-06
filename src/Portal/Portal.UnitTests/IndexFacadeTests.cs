using System;
using System.Collections.Generic;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Calculators;
using Portal.Configuration;
using Portal.Helpers;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class IndexFacadeTests
    {
        private IDmEndDateCalculator _dmEndDateCalculator;
        private IOnHoldCalculator _onHoldCalculator;
        private IIndexFacade _indexFacade;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;

        [SetUp]
        public void SetUp()
        {
            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.DmEndDateDays = 14;

            _dmEndDateCalculator = new DmEndDateCalculator(_generalConfig);
            _onHoldCalculator = new OnHoldCalculator();

            _indexFacade = new IndexFacade(_dmEndDateCalculator, _onHoldCalculator);
        }

        [Test]
        public void Test_IndexFacade_Returns_Correct_DmEndDate_Including_OnHold_Days()
        {
            var onHoldDays = 3;

            // We'll add 3 on hold days to the DmEndDate
            var onHoldRows = new List<OnHold>
            {
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-onHoldDays),
                    OnHoldUser = "TestUser",
                    OffHoldTime = null,
                    OffHoldUser = null,
                }
            };

            var effectiveDate = DateTime.Today;
            var result = _indexFacade.CalculateDmEndDate(effectiveDate, onHoldRows);

            Assert.AreEqual(effectiveDate.AddDays(_generalConfig.Value.DmEndDateDays).AddDays(onHoldDays), result.dmEndDate);
            Assert.AreEqual(_generalConfig.Value.DmEndDateDays + onHoldDays, result.daysToDmEndDate);

        }

        [Test]
        public void Test_IndexFacade_Returns_Correct_DmEndDate_When_There_Are_No_OnHold_Days()
        {
            var onHoldRows = new List<OnHold>();
            var effectiveDate = DateTime.Today;


            var result = _indexFacade.CalculateDmEndDate(effectiveDate, onHoldRows);
            Assert.AreEqual(effectiveDate.AddDays(_generalConfig.Value.DmEndDateDays), result.dmEndDate);
            Assert.AreEqual(_generalConfig.Value.DmEndDateDays, result.daysToDmEndDate);

        }
    }
}
