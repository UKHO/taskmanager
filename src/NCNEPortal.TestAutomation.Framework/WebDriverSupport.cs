using System;
using BoDi;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Framework
{
    [Binding]
    public sealed class WebDriverSupport : IDisposable
    {
        private readonly IObjectContainer _objectContainer;
        private IWebDriver _webDriver;

        public WebDriverSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        public void Dispose()
        {
            DisposeWebdriver();
        }

        [BeforeScenario(Order = 0)]
        public void InitializeWebDriver()
        {
            _webDriver = new ChromeDriver(Environment.GetEnvironmentVariable("ChromeWebDriver"));
            _objectContainer.RegisterInstanceAs(_webDriver);
        }

        [AfterScenario]
        public void DisposeWebdriver()
        {
            _webDriver?.Quit();
            _webDriver?.Dispose();
            _webDriver = null;
        }
    }
}