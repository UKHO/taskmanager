using System;
using BoDi;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
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
            var chromeDriverDirectory = Environment.GetEnvironmentVariable("ChromeWebDriver");
            if (string.IsNullOrEmpty(chromeDriverDirectory))
                throw new ApplicationException("Missing environment variable: ChromeWebDriver");

            try
            {
                _webDriver = new ChromeDriver(chromeDriverDirectory);
                _objectContainer.RegisterInstanceAs(_webDriver);
                _objectContainer.RegisterInstanceAs(new WebDriverWait(_webDriver, TimeSpan.FromSeconds(10)));
            }
            catch
            {
                DisposeWebdriver();
                throw;
            }
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