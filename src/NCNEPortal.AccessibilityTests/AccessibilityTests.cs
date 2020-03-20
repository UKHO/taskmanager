using System;
using BoDi;
using NCNEPortal.TestAutomation.Framework;
using NCNEPortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.AccessibilityTests
{
    [TestFixture]
    public class AccessibilityTests : IDisposable
    {
        [SetUp]
        public void Setup()
        {
            _configSupport.RegisterConfigs();

            _webDriverSupport.InitializeWebDriver();
            _webDriverSupport.SetLoginCookies();

            _wait = _objectContainer.Resolve<WebDriverWait>();
            _driver = _objectContainer.Resolve<IWebDriver>();
        }

        [TearDown]
        public void TearDown()
        {
            _webDriverSupport.DisposeWebdriver();
        }

        private readonly ConfigSupport _configSupport;
        private readonly IObjectContainer _objectContainer;
        private readonly WebDriverSupport _webDriverSupport;
        private IWebDriver _driver;
        private WebDriverWait _wait;

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

        [Test]
        public void LandingPageIsAccessible()
        {
            var landingPage = _objectContainer.Resolve<LandingPage>();
            landingPage.NavigateTo();

            Assert.IsTrue(landingPage.HasLoaded);

            var axePageEvaluator = new AxePageEvaluator((IJavaScriptExecutor) _driver);
            var axeResult = axePageEvaluator.GetAxeResults();
            
            CollectionAssert.IsEmpty(axeResult.Violations);
        }
    }
}