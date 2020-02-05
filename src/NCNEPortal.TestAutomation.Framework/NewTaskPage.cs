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
        private readonly Uri _newtaskPageUrl;
        private readonly NewTaskPageConfig _config = new NewTaskPageConfig();

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public NewTaskPage(IWebDriver driver)
        {
            var configRoot = AzureAppConfigConfigurationRoot.Instance;
            configRoot.GetSection("urls").Bind(_config);

            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(SeleniumTimeoutSeconds));

            _newtaskPageUrl = ConfigHelpers.IsAzureDevOpsBuild
                ? _config.NcneNewTaskPageUrl
                : _config.NcneLocalDevNewTaskPageUrl;
              
        }

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
