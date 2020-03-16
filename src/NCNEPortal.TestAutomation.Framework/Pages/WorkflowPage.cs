using System;
using Common.Helpers;
using NCNEPortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework.Pages
{
    public class WorkflowPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly Uri _workflowPageUrl;

        public WorkflowPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
        {
            _driver = driver;
            _wait = wait;

            _workflowPageUrl = ConfigHelpers.IsAzureDevOpsBuild
                ? urlsConfig.NcneWorkflowPageUrl
                : urlsConfig.NcneLocalDevWorkflowPageUrl;
        }

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public void NavigateTo()
        {
            _driver.Navigate().GoToUrl(_workflowPageUrl);
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