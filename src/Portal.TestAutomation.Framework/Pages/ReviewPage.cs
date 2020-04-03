using System;
using Common.Helpers;
using Common.TestAutomation.Framework.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Portal.TestAutomation.Framework.Configuration;

namespace Portal.TestAutomation.Framework.Pages
{
    public class ReviewPage : PageBase
    {
        public ReviewPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
            : base(driver,
                wait,
                ConfigHelpers.IsAzureDevOpsBuild
                    ? urlsConfig.ReviewPageUrl
                    : urlsConfig.LocalDevReviewPageUrl)
        {
        }

        private IWebElement ReviewForm => Driver.FindElement(By.Id("frmReviewPage"));

        private IWebElement SourceDocumentTable => Driver.FindElement(By.Id("srcDocDetailsTable"));

        public override bool HasLoaded
        {
            get
            {
                try
                {
                    Wait.Until(driver => ReviewForm.Displayed);
                    return true;
                }
                catch (NoSuchElementException e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }
        }

        public void NavigateToProcessId(int processId)
        {
            Driver.Navigate().GoToUrl($"{PageUrl}?processId={processId}");
        }

        public bool IsSdocIdInDetails(int sDocId)
        {
            int.TryParse(SourceDocumentTable.FindElement(By.XPath("//tbody/tr/td[3]")).Text, out var sDocOnPage);
            return sDocId == sDocOnPage;
        }
    }
}