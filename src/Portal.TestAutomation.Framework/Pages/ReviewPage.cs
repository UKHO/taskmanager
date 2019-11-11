using System;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Portal.TestAutomation.Framework.Configuration;

namespace Portal.TestAutomation.Framework.Pages
{
    public class ReviewPage : BasePage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly ReviewPageConfig _config = new ReviewPageConfig();

        //add below to azure config values
        private Uri ReviewPageUrl;

        private IWebElement UkhoLogo => _driver.FindElement(By.Id("ukhoLogo"));

        private IWebElement SourceDocumentTable => _driver.FindElement(By.XPath("//*[@id='srcDocDetailsTable']"));

        public ReviewPage(IWebDriver driver, int seconds)
        {
            var configRoot = AzureAppConfigConfigurationRoot.Instance;
            configRoot.GetSection("urls").Bind(_config);

            ReviewPageUrl = ConfigHelpers.IsAzureDevOpsBuild ? _config.ReviewPageUrl : _config.LocalDevReviewPageUrl;

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
            _driver.Navigate().GoToUrl(ReviewPageUrl);
            _driver.Manage().Window.Maximize();
        }

        public void NavigateToProcessId(int processId)
        {
            _driver.Navigate().GoToUrl(ReviewPageUrl + "?processId=" + processId);
            _driver.Manage().Window.Maximize();
        }

        public bool IsSdocIdInDetails(int sDocId)
        {
            int.TryParse(SourceDocumentTable.FindElement(By.XPath("//tbody/tr/td[3]")).Text, out var sDocOnPage);
            return sDocId == sDocOnPage;
        }

    }
}
