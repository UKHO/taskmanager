using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework
{
    public class LandingPage
    {
        private readonly IWebDriver _driver;
        private WebDriverWait _wait;
        private const int SeleniumTimeoutSeconds = 5;

        public LandingPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(SeleniumTimeoutSeconds));
        }

        public void NavigateTo()
        {
            throw new NotImplementedException();
        }

        public void HasLoaded()
        {
            throw new NotImplementedException();
        }
    }
}