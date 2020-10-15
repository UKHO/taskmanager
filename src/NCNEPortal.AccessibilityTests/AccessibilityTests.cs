using BoDi;
using Common.TestAutomation.Framework;
using Common.TestAutomation.Framework.Axe;
using Common.TestAutomation.Framework.Logging;
using NCNEPortal.TestAutomation.Framework;
using NCNEPortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using System;

namespace NCNEPortal.AccessibilityTests
{
    [TestFixture]
    public sealed class AccessibilityTests : IDisposable
    {
        private readonly ConfigSupport _configSupport;
        private readonly IObjectContainer _objectContainer;
        private readonly WebDriverSupport _webDriverSupport;
        private AxeResultAnalyser _axeResultAnalyser;
        private AxePageEvaluator _axePageEvaluator;

        public AccessibilityTests()
        {
            _objectContainer = new ObjectContainer();

            _configSupport = _objectContainer.Resolve<ConfigSupport>();
            _webDriverSupport = _objectContainer.Resolve<WebDriverSupport>();
        }

        public void Dispose()
        {
            _webDriverSupport?.DisposeWebdriver();
        }

        [OneTimeSetUp]
        public void BeforeAllTests()
        {
            _objectContainer.RegisterTypeAs<NunitTestLogging, ITestLogging>();
            ConfigSupport.PopulateConfigsFromAzure();

            _configSupport.RegisterAzureConfigs();
            _configSupport.RegisterLandingPage();
            _configSupport.RegisterWorkflowPage();
            _configSupport.RegisterNewTaskPage();
            _configSupport.RegisterWithdrawalPage();

            _webDriverSupport.InitializeWebDriver();
            _webDriverSupport.SetLoginCookies();

            _axePageEvaluator = _objectContainer.Resolve<AxePageEvaluator>();
            _axeResultAnalyser = _objectContainer.Resolve<AxeResultAnalyser>();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _webDriverSupport.DisposeWebdriver();
        }

        [Test]
        public void LandingPageIsAccessible()
        {
            var landingPage = _objectContainer.Resolve<LandingPage>();
            landingPage.NavigateTo();

            Assert.IsTrue(landingPage.HasLoaded);

            var axeResult = _axePageEvaluator.GetAxeResults();

            _axeResultAnalyser.AssertAxeViolations(axeResult);
        }

        [Test]
        public void NewTaskPageIsAccessible()
        {
            var newTaskPage = _objectContainer.Resolve<NewTaskPage>();
            newTaskPage.NavigateTo();

            Assert.IsTrue(newTaskPage.HasLoaded);

            var axeResult = _axePageEvaluator.GetAxeResults();

            _axeResultAnalyser.AssertAxeViolations(axeResult);
        }

        [Test]
        public void WithdrawalPageIsAccessible()
        {
            var withdrawalPage = _objectContainer.Resolve<WithdrawalPage>();
            withdrawalPage.NavigateTo();

            Assert.IsTrue(withdrawalPage.HasLoaded);

            var axeResult = _axePageEvaluator.GetAxeResults();

            _axeResultAnalyser.AssertAxeViolations(axeResult);

        }

        [Test]
        public void WorkflowPageIsAccessible()
        {
            var workflowPage = _objectContainer.Resolve<WorkflowPage>();

            // For now, relies on deployment re-seeding UAT
            workflowPage.NavigateToProcessId(6);

            Assert.IsTrue(workflowPage.HasLoaded);

            var axeResult = _axePageEvaluator.GetAxeResults();

            _axeResultAnalyser.AssertAxeViolations(axeResult);
        }
    }
}