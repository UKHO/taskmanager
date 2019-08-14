using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using Portal.TestAutomation.Framework.Pages.Configurations;

namespace Portal.TestAutomation.Framework.Pages
{
    public class LandingPage : BasePage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly LandingPageConfig _config = new LandingPageConfig();

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        public LandingPage(IWebDriver driver, int seconds)
        {
            var configRoot = ConfigurationRoot.Instance;
            configRoot.GetSection("urls").Bind(_config);

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
            _driver.Navigate().GoToUrl(_config.LandingPageUrl);
        }
    }
}
