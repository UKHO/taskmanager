using Common.TestAutomation.Framework.Pages;
using DbUpdatePortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;


namespace DbUpdatePortal.TestAutomation.Framework.Pages
{
    public class HistoricalTaskPage : PageBase, IHistoricalTaskPage
    {
        public HistoricalTaskPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
            : base(driver, wait, urlsConfig.DbUpdateHistoricalTasksPageUrl)
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
    }
}
