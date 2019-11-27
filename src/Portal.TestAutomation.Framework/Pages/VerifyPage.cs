using System;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Portal.TestAutomation.Framework.Configuration;

namespace Portal.TestAutomation.Framework.Pages
{
    public class VerifyPage : BasePage

    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly VerifyPageConfig _config = new VerifyPageConfig();

        //add below to azure config values
        private Uri VerifyPageUrl;

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public VerifyPage(IWebDriver driver, int seconds)
        {
            var configRoot = AzureAppConfigConfigurationRoot.Instance;
            configRoot.GetSection("urls").Bind(_config);

            VerifyPageUrl = ConfigHelpers.IsAzureDevOpsBuild ? _config.VerifyPageUrl : _config.LocalDevVerifyPageUrl;

            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(seconds));
        }

        public override bool HasLoaded()
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

        public void NavigateTo()
        {
            _driver.Navigate().GoToUrl(VerifyPageUrl);
            _driver.Manage().Window.Maximize();
        }
    }
}


