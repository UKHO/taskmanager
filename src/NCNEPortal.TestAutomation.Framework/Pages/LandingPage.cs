using System;
using Common.Helpers;
using NCNEPortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework.Pages
{
    public class LandingPage
    {
        private readonly IWebDriver _driver;
        private readonly Uri _landingPageUrl;
        private readonly WebDriverWait _wait;

        public LandingPage(IWebDriver driver, WebDriverWait wait, UrlsConfig config)
        {
            _driver = driver;
            _wait = wait;

            _landingPageUrl = ConfigHelpers.IsAzureDevOpsBuild
                ? config.NcneLandingPageUrl
                : config.NcneLocalDevLandingPageUrl;
        }

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public void NavigateTo()
        {
            _driver.Navigate().GoToUrl(_landingPageUrl);
            _driver.Manage().Window.Maximize();
        }
        
        public bool HasLoaded()
        {
            try
            {
                _wait.Until(driver => UkhoLogo.Displayed);
                return true;
            }
            catch (NoSuchElementException e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}