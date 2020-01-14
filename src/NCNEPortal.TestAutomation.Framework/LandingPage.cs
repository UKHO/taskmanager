using System;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework
{
    public class LandingPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private const int SeleniumTimeoutSeconds = 5;
        private readonly Uri _landingPageUrl;
        private readonly LandingPageConfig _config = new LandingPageConfig();

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public LandingPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(SeleniumTimeoutSeconds));
            
            // TODO: replace with NCNE URI from config
            _config = new LandingPageConfig{LandingPageUrl = new Uri("https://www.google.co.uk"), LocalDevLandingPageUrl = new Uri("https://localhost:44329/") };

            _landingPageUrl = ConfigHelpers.IsAzureDevOpsBuild ? _config.LandingPageUrl : _config.LocalDevLandingPageUrl;

        }

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