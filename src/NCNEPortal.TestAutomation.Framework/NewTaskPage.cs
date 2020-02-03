using System;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
namespace NCNEPortal.TestAutomation.Framework
{
    public class NewTaskPage

    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private const int SeleniumTimeoutSeconds = 5;
        private readonly Uri _workflowPageUrl;
        private readonly WorkflowPageConfig _config = new WorkflowPageConfig();

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public NewTaskPage(IWebDriver driver)
        {
            var configRoot = AzureAppConfigConfigurationRoot.Instance;
            configRoot.GetSection("urls").Bind(_config);

            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(SeleniumTimeoutSeconds));

            _workflowPageUrl = ConfigHelpers.IsAzureDevOpsBuild
                ? _config.NcneWorkflowPageUrl
                : _config.NcneLocalDevWorkflowPageUrl;
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
