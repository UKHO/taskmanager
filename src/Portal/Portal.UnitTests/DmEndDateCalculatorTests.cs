using System;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Calculators;
using Portal.Configuration;

namespace Portal.UnitTests
{
    public class DmEndDateCalculatorTests
    {
        private DmEndDateCalculator _dmEndDateCalculator;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;

        [SetUp]
        public void SetUp()
        {
            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.DmEndDateDaysSimple = 14;
            _generalConfig.Value.DmEndDateDaysLTA = 72;
            _generalConfig.Value.DaysToDmEndDateRedAlertUpperInc = 0;
            _generalConfig.Value.DaysToDmEndDateAmberAlertUpperInc = 2;

            _dmEndDateCalculator = new DmEndDateCalculator(_generalConfig);
        }

        [TestCase("Review", "Simple", 14)]
        [TestCase("Review", "LTA", 14)]
        [TestCase("Review", "LTA (Product only)", 14)]
        [TestCase("Review", "", 14)]
        [TestCase("Assess", "Simple", 14)]
        [TestCase("Assess", "LTA", 72)]
        [TestCase("Assess", "LTA (Product only)", 72)]
        [TestCase("Assess", "", 72)]
        [TestCase("Verify", "Simple", 14)]
        [TestCase("Verify", "LTA", 72)]
        [TestCase("Verify", "LTA (Product only)", 72)]
        [TestCase("Verify", "", 72)]
        public void Test_CalculateDmEndDate_Given_Valid_Data_Then_Returns_Correct_DmEndDate(string taskStage,
            string taskType, int expectedDmEndDateDays)
        {
            var effectiveDate = DateTime.Today;
            var result = _dmEndDateCalculator.CalculateDmEndDate(effectiveDate, taskType, taskStage);


            Assert.AreEqual(effectiveDate.AddDays(expectedDmEndDateDays), result.dmEndDate);
            Assert.AreEqual(expectedDmEndDateDays, result.daysToDmEndDate);

        }

        [TestCase(3)]
        [TestCase(short.MaxValue)]
        public void Test_DetermineDaysToDmEndDateAlerts_Returns_No_Alerts(short daysToDmEndDate)
        {
            var result = _dmEndDateCalculator.DetermineDaysToDmEndDateAlerts(daysToDmEndDate);

            Assert.AreEqual(false, result.amberAlert);
            Assert.AreEqual(false, result.redAlert);
        }

        [TestCase(2)]
        [TestCase(1)]
        public void Test_DetermineDaysToDmEndDateAlerts_Returns_Amber_Alert_Only(short daysToDmEndDate)
        { 
            var result = _dmEndDateCalculator.DetermineDaysToDmEndDateAlerts(daysToDmEndDate);

            Assert.AreEqual(true, result.amberAlert);
            Assert.AreEqual(false, result.redAlert);
        }

        [TestCase(0)]
        [TestCase(short.MinValue)]
        public void Test_DetermineDaysToDmEndDateAlerts_Returns_Red_Alert_Only(short daysToDmEndDate)
        { 
            var result = _dmEndDateCalculator.DetermineDaysToDmEndDateAlerts(daysToDmEndDate);

            Assert.AreEqual(true, result.redAlert);
            Assert.AreEqual(false, result.amberAlert);
        }
    }
}
