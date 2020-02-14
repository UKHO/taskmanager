using System;
using BoDi;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Framework.Driver
{
    [Binding]
    internal sealed class WebDriverSupport
    {
        private readonly IObjectContainer _objectContainer;

        public WebDriverSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 0)]
        public void InitializeWebDriver()
        {
            var chromeDriverDirectory = Environment.GetEnvironmentVariable("ChromeWebDriver");
            if (string.IsNullOrEmpty(chromeDriverDirectory)) throw new ApplicationException("Missing environment variable: ChromeWebDriver");

            var webDriver = new ChromeDriver(chromeDriverDirectory);
            _objectContainer.RegisterInstanceAs<IWebDriver>(webDriver);
        }
    }
}
