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
    }
}
