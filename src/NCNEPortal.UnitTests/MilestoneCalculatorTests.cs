using FakeItEasy;
using Microsoft.Extensions.Options;
using NCNEPortal.Calculators;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using NUnit.Framework;
using System;

namespace NCNEPortal.UnitTests
{
    public class MilestoneCalculatorTests
    {

        private MilestoneCalculator _milestoneCalculator;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;

        [SetUp]
        public void Setup()
        {
            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.FormsDaysFromPubDate = -36;
            _generalConfig.Value.CisDaysFromPubDate = -6;
            _generalConfig.Value.Commit2WDaysFromPubDate = -15;
            _generalConfig.Value.Commit3WDaysFromPubDate = -21;
            _milestoneCalculator = new MilestoneCalculator(_generalConfig);
        }

        [Test]
        public void Test_Milestone_Calculator_for_TwoWeeks_Deadline()
        {
            var deadLine = DeadlineEnum.TwoWeeks;
            var publicationDate = DateTime.Today;

            var result = _milestoneCalculator.CalculateMilestones(deadLine, publicationDate);

            Assert.AreEqual(result.formsDate.AddDays(36), publicationDate);
            Assert.AreEqual(result.cisDate.AddDays(6), publicationDate);
            Assert.AreEqual(result.commitDate.AddDays(15), publicationDate);
        }

        [Test]
        public void Test_Milestone_Calculator_for_ThreeWeeks_Deadline()
        {
            var deadLine = DeadlineEnum.ThreeWeeks;
            var publicationDate = DateTime.Today;

            var result = _milestoneCalculator.CalculateMilestones(deadLine, publicationDate);

            Assert.AreEqual(result.formsDate.AddDays(36), publicationDate);
            Assert.AreEqual(result.cisDate.AddDays(6), publicationDate);
            Assert.AreEqual(result.commitDate.AddDays(21), publicationDate);
        }
    }
}