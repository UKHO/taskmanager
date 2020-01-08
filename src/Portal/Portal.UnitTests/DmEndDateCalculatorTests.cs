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
            _generalConfig.Value.DmEndDateDays = 14;
            _generalConfig.Value.DaysToDmEndDateRedAlertUpperInc = 0;
            _generalConfig.Value.DaysToDmEndDateAmberAlertUpperInc = 2;

            _dmEndDateCalculator = new DmEndDateCalculator(_generalConfig);
        }

        [Test]
        public void Test_DmEndDateCalculator_Returns_Correct_DmEndDate()
        {
            var effectiveDate = DateTime.Today;
            var result = _dmEndDateCalculator.CalculateDmEndDate(effectiveDate);


            Assert.AreEqual(effectiveDate.AddDays(_generalConfig.Value.DmEndDateDays), result.dmEndDate);
            Assert.AreEqual(_generalConfig.Value.DmEndDateDays, result.daysToDmEndDate);

        }

        [Test]
        public void Test_DetermineDaysToDmEndDateAlerts_Returns_No_Alerts()
        {
            var result = _dmEndDateCalculator.DetermineDaysToDmEndDateAlerts(3);

            Assert.AreEqual(false, result.amberAlert);
            Assert.AreEqual(false, result.redAlert);
        }

        [Test]
        public void Test_DetermineDaysToDmEndDateAlerts_Returns_Amber_Alert_Only()
        { 
            var result = _dmEndDateCalculator.DetermineDaysToDmEndDateAlerts(2);

            Assert.AreEqual(true, result.amberAlert);
            Assert.AreEqual(false, result.redAlert);
        }

        [Test]
        public void Test_DetermineDaysToDmEndDateAlerts_Returns_Red_Alert_Only()
        { 
            var result = _dmEndDateCalculator.DetermineDaysToDmEndDateAlerts(0);

            Assert.AreEqual(true, result.redAlert);
            Assert.AreEqual(false, result.amberAlert);
        }
    }
}
