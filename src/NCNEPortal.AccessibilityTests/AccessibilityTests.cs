using System;
using BoDi;
using Common.TestAutomation.Framework;
using Common.TestAutomation.Framework.Axe;
using Common.TestAutomation.Framework.Logging;
using NCNEPortal.TestAutomation.Framework;
using NCNEPortal.TestAutomation.Framework.Pages;
using NUnit.Framework;

namespace NCNEPortal.AccessibilityTests
{
    [TestFixture]
    public class AccessibilityTests : IDisposable
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
        public static void BeforeAllTests()
        {
            ConfigSupport.PopulateConfigsFromAzure();
        }

        [SetUp]
        public void Setup()
        {
            _objectContainer.RegisterTypeAs<NunitTestLogging, ITestLogging>();

            _configSupport.RegisterAzureConfigs();
            _configSupport.RegisterLandingPage();

            _webDriverSupport.InitializeWebDriver();
            _webDriverSupport.SetLoginCookies();

            _axePageEvaluator = _objectContainer.Resolve<AxePageEvaluator>();
            _axeResultAnalyser = _objectContainer.Resolve<AxeResultAnalyser>();
        }

        [TearDown]
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
    }
}