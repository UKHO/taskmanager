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

        [SetUp]
        public void SetUp()
        {
            var generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            generalConfig.Value.DmEndDateDays = 14;

            _dmEndDateCalculator = new DmEndDateCalculator(generalConfig);
            _onHoldCalculator = new OnHoldCalculator();

            _indexFacade = new IndexFacade(_dmEndDateCalculator, _onHoldCalculator);
        }

        [Test]
        public void Test_IndexFacade_Returns_Correct_DmEndDate_Including_OnHold_Days()
        {
            // We'll add 3 on hold days to the DmEndDate
            var onHoldRows = new List<OnHold>
            {
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-3),
                    OnHoldUser = "TestUser",
                    OffHoldTime = null,
                    OffHoldUser = null,
                }
            };

            Assert.AreEqual(new DateTime(2020, 01, 18), _indexFacade.CalculateDmEndDate(new DateTime(2020, 1, 1), onHoldRows));
        }

        [Test]
        public void Test_IndexFacade_Returns_Correct_DmEndDate_When_There_Are_No_OnHold_Days()
        {
            var onHoldRows = new List<OnHold>();

            Assert.AreEqual(new DateTime(2020, 01, 15), _indexFacade.CalculateDmEndDate(new DateTime(2020, 1, 1), onHoldRows));
        }
    }
}
