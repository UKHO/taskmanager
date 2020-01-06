using System;
using System.Collections.Generic;
using NUnit.Framework;
using Portal.Calculators;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class OnHoldCalculatorTests
    {
        private IList<OnHold> _onHoldRows;
        private OnHoldCalculator _onHoldCalculator;

        [SetUp]
        public void SetUp()
        {
            _onHoldCalculator = new OnHoldCalculator();
        }

        [Test]
        public void Test_OnHoldCalculator_Returns_Correct_Number_Of_Days_When_Task_Put_On_Hold_3_Days_Ago_And_Still_On_Hold()
        {
            _onHoldRows = new List<OnHold>
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

            var amount = _onHoldCalculator.CalculateOnHoldDays(_onHoldRows, DateTime.Now.Date);
            Assert.AreEqual(3, amount);
        }

        [Test]
        public void Test_OnHoldCalculator_Returns_Correct_Number_Of_Days_When_Task_Put_On_Hold_And_Taken_Off_Hold()
        {
            _onHoldRows = new List<OnHold>
            {
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-3),
                    OnHoldUser = "TestUser",
                    OffHoldTime = DateTime.Now.Date.AddDays(-1),
                    OffHoldUser = "TestUser"
                }
            };

            var amount = _onHoldCalculator.CalculateOnHoldDays(_onHoldRows, DateTime.Now.Date);
            Assert.AreEqual(2, amount);
        }

        [Test]
        public void Test_OnHoldCalculator_Returns_Correct_Number_Of_Days_When_Task_Has_Several_On_Hold_Rows_And_Is_On_Hold()
        {
            _onHoldRows = new List<OnHold>
            {
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-3),
                    OnHoldUser = "TestUser",
                    OffHoldTime = DateTime.Now.Date.AddDays(-2),
                    OffHoldUser = "TestUser",
                },
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-2),
                    OnHoldUser = "TestUser",
                    OffHoldTime = DateTime.Now.Date.AddDays(-2),
                    OffHoldUser = "TestUser",
                },
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-1),
                    OnHoldUser = "TestUser",
                    OffHoldTime = null,
                    OffHoldUser = null,
                },
            };

            var amount = _onHoldCalculator.CalculateOnHoldDays(_onHoldRows, DateTime.Now.Date);
            Assert.AreEqual(2, amount);
        }

        [Test]
        public void Test_OnHoldCalculator_Returns_Correct_Number_Of_Days_When_Task_Has_Several_On_Hold_Rows_And_Is_Off_Hold()
        {
            _onHoldRows = new List<OnHold>
            {
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-3),
                    OnHoldUser = "TestUser",
                    OffHoldTime = DateTime.Now.Date.AddDays(-2),
                    OffHoldUser = "TestUser",
                },
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-2),
                    OnHoldUser = "TestUser",
                    OffHoldTime = DateTime.Now.Date.AddDays(-2),
                    OffHoldUser = "TestUser",
                },
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-1),
                    OnHoldUser = "TestUser",
                    OffHoldTime = DateTime.Now.Date.AddDays(-1),
                    OffHoldUser = "TestUser",
                },
            };

            var amount = _onHoldCalculator.CalculateOnHoldDays(_onHoldRows, DateTime.Now.Date);
            Assert.AreEqual(1, amount);
        }

        [Test]
        public void Test_OnHoldCalculator_Returns_Correct_Number_Of_Days_When_Task_Put_On_Hold_And_Taken_Off_Hold_On_The_Same_Day()
        {
            _onHoldRows = new List<OnHold>
            {
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-1),
                    OnHoldUser = "TestUser",
                    OffHoldTime = DateTime.Now.Date.AddDays(-1),
                    OffHoldUser = "TestUser"
                }
            };

            var amount = _onHoldCalculator.CalculateOnHoldDays(_onHoldRows, DateTime.Now.Date);
            Assert.AreEqual(0, amount);
        }
    }
}
