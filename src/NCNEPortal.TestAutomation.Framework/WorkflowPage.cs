using System;
using Common.Helpers;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework
{
    public class WorkflowPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private const int SeleniumTimeoutSeconds = 5;
        private readonly Uri _workflowPageUrl;
        private readonly WorkflowPageConfig _config = new WorkflowPageConfig();

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public WorkflowPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(SeleniumTimeoutSeconds));

            // TODO: replace with NCNE URI from config
            _config = new WorkflowPageConfig
            {
                WorkflowPageUrl = new Uri("https://www.google.co.uk/"),
                LocalDevWorkflowPageUrl = new Uri("https://taskmanager-dev-web-ncneportal.azurewebsites.net/Workflow?ProcessId=2")
            };

            _workflowPageUrl = ConfigHelpers.IsAzureDevOpsBuild
                ? _config.WorkflowPageUrl
                : _config.LocalDevWorkflowPageUrl;

        }

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