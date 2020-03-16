using System;
using Common.Helpers;
using NCNEPortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework.Pages
{
    public class NewTaskPage

    {
        private readonly IWebDriver _driver;
        private readonly Uri _newtaskPageUrl;
        private readonly WebDriverWait _wait;

        public NewTaskPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
        {
            _driver = driver;
            _wait = wait;

            _newtaskPageUrl = ConfigHelpers.IsAzureDevOpsBuild
                ? urlsConfig.NcneNewTaskPageUrl
                : urlsConfig.NcneLocalDevNewTaskPageUrl;
        }

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public void NavigateTo()
        {
            _driver.Navigate().GoToUrl(_newtaskPageUrl);
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