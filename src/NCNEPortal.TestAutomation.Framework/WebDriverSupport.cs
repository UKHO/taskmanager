using System;
using BoDi;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Framework
{
    [Binding]
    internal sealed class WebDriverSupport
    {
        private readonly IObjectContainer _objectContainer;
        private IWebDriver _webDriver;

        public WebDriverSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 0)]
        public void InitializeWebDriver()
        {
            _webDriver = new ChromeDriver(Environment.GetEnvironmentVariable("ChromeWebDriver"));
            _objectContainer.RegisterInstanceAs(_webDriver);
        }

        ~WebDriverSupport()
        {
            _webDriver?.Quit();
            _webDriver = null;
        }
    }
}