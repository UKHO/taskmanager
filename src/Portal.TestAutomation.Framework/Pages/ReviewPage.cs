using System;
using System.Linq;
using Common.Helpers;
using Common.TestAutomation.Framework.PageElements;
using Common.TestAutomation.Framework.Pages;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Portal.TestAutomation.Framework.Configuration;

namespace Portal.TestAutomation.Framework.Pages
{
    public class ReviewPage : PageBase
    {
        private readonly Table _sourceDocumentTable;

        public ReviewPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
            : base(driver,
                wait,
                ConfigHelpers.IsAzureDevOpsBuild
                    ? urlsConfig.ReviewPageUrl
                    : urlsConfig.LocalDevReviewPageUrl)
        {
            _sourceDocumentTable = new Table(Driver, By.Id("srcDocDetailsTable"));
        }

        private IWebElement ReviewForm => Driver.FindElement(By.Id("frmReviewPage"));

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
            return _sourceDocumentTable["SDOC"].Contains(sDocId.ToString());
        }
    }
}