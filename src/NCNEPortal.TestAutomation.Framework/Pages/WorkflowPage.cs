using System;
using Common.TestAutomation.Framework.Pages;
using NCNEPortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework.Pages
{
    public class WorkflowPage : PageBase, IWorkflowPage
    {
        public WorkflowPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
            : base(driver, wait, urlsConfig.NcneWorkflowPageUrl)
        {
        }

        private IWebElement UkhoLogo => Driver.FindElement(By.Id("ukhoLogo"));

        public override bool HasLoaded
        {
            get
            {
                try
                {
                    Wait.Until(driver => UkhoLogo.Displayed);
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
            var uriBuilder = new UriBuilder(PageUrl);

            var queryToAppend = $"ProcessId={processId}";

            if (uriBuilder.Query?.Length > 1)
                uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + queryToAppend;
            else
                uriBuilder.Query = queryToAppend;

            Driver.Navigate().GoToUrl(uriBuilder.ToString());
        }
    }
}