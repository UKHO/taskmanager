using NCNEPortal.Calculators;
using NUnit.Framework;
using System;
using NCNEPortal.Enums;

namespace NCNEPortal.UnitTests
{
    public class MilestoneCalculatorTests
    {

        private MilestoneCalculator _milestoneCalculator;

        [SetUp]
        public void Setup()
        {
            _milestoneCalculator = new MilestoneCalculator();
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