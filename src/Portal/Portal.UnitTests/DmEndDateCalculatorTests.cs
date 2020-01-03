using System;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Configuration;

namespace Portal.UnitTests
{
    public class DmEndDateCalculatorTests
    {
        private DmEndDateCalculator _dmEndDateCalculator;

        [SetUp]
        public void SetUp()
        {
            var generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            generalConfig.Value.DmEndDateDays = 14;

            _dmEndDateCalculator = new DmEndDateCalculator(generalConfig);
        }

        [Test]
        public void Test_DmEndDateCalculator_Returns_Correct_DmEndDate()
        {
            Assert.AreEqual(new DateTime(2020, 01, 15), 
                _dmEndDateCalculator.CalculateDmEndDate(new DateTime(2020, 1, 1)));
        }
    }
}
