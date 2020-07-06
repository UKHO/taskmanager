using System;
using System.Collections.Generic;
using System.Linq;
using Common.Helpers;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Calculators;
using Portal.Configuration;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class OnHoldCalculatorTests
    {
        private IList<OnHold> _onHoldRows;
        private OnHoldCalculator _onHoldCalculator;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;
        private WorkflowDbContext _dbContext;

        public AdUser TestUser
        {
            get
            {
                var user = AdUser.Unknown;

                user = _dbContext.AdUsers.SingleOrDefault(u =>
                    u.UserPrincipalName.Equals("test@email.com", StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    user = new AdUser
                    {
                        DisplayName = "Test User",
                        UserPrincipalName = "test@email.com"
                    };
                    _dbContext.SaveChanges();
                }

                return user;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _dbContext = DatabasesHelpers.GetInMemoryWorkflowDbContext();
            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.OnHoldDaysGreenIconUpper = 5;
            _generalConfig.Value.OnHoldDaysAmberIconUpper = 6;
            _generalConfig.Value.OnHoldDaysRedIconUpper = 7;
            _onHoldCalculator = new OnHoldCalculator(_generalConfig);
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
                    OnHoldBy = TestUser,
                    OffHoldTime = null,
                    OffHoldBy = null,
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
                    OnHoldBy = TestUser,
                    OffHoldTime = DateTime.Now.Date.AddDays(-1),
                    OffHoldBy = TestUser
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
                    OnHoldBy = TestUser,
                    OffHoldTime = DateTime.Now.Date.AddDays(-2),
                    OffHoldBy = TestUser,
                },
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-2),
                    OnHoldBy = TestUser,
                    OffHoldTime = DateTime.Now.Date.AddDays(-2),
                    OffHoldBy = TestUser,
                },
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-1),
                    OnHoldBy = TestUser,
                    OffHoldTime = null,
                    OffHoldBy = null,
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
                    OnHoldBy = TestUser,
                    OffHoldTime = DateTime.Now.Date.AddDays(-2),
                    OffHoldBy = TestUser,
                },
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-2),
                    OnHoldBy = TestUser,
                    OffHoldTime = DateTime.Now.Date.AddDays(-2),
                    OffHoldBy = TestUser,
                },
                new OnHold
                {
                    ProcessId = 123,
                    WorkflowInstanceId = 1,
                    OnHoldTime = DateTime.Now.Date.AddDays(-1),
                    OnHoldBy = TestUser,
                    OffHoldTime = DateTime.Now.Date.AddDays(-1),
                    OffHoldBy = TestUser,
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
                    OnHoldBy = TestUser,
                    OffHoldTime = DateTime.Now.Date.AddDays(-1),
                    OffHoldBy = TestUser
                }
            };

            var amount = _onHoldCalculator.CalculateOnHoldDays(_onHoldRows, DateTime.Now.Date);
            Assert.AreEqual(0, amount);
        }

        [Test]
        public void Test_OnHoldCalculator_Returns_GreenIcon_If_Task_Is_Under_Threshold()
        {
            var (greenIcon, amberIcon, redIcon) = _onHoldCalculator.DetermineOnHoldDaysIcons(_generalConfig.Value.OnHoldDaysGreenIconUpper);
            Assert.AreEqual(true, greenIcon);
            Assert.AreEqual(false, amberIcon);
            Assert.AreEqual(false, redIcon);
        }

        [Test]
        public void Test_OnHoldCalculator_Returns_AmberIcon_If_Task_Is_At_Threshold()
        {
            var (greenIcon, amberIcon, redIcon) = _onHoldCalculator.DetermineOnHoldDaysIcons(_generalConfig.Value.OnHoldDaysAmberIconUpper);
            Assert.AreEqual(false, greenIcon);
            Assert.AreEqual(true, amberIcon);
            Assert.AreEqual(false, redIcon);
        }

        [Test]
        public void Test_OnHoldCalculator_Returns_RedIcon_If_Task_Is_Above_Threshold()
        {
            var (greenIcon, amberIcon, redIcon) = _onHoldCalculator.DetermineOnHoldDaysIcons(_generalConfig.Value.OnHoldDaysRedIconUpper);
            Assert.AreEqual(false, greenIcon);
            Assert.AreEqual(false, amberIcon);
            Assert.AreEqual(true, redIcon);
        }
    }
}
