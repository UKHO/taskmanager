using System;
using Common.TestAutomation.Framework.Pages;
using DbUpdatePortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace DbUpdatePortal.TestAutomation.Framework.Pages
{
    public class NewTaskPage : PageBase, INewTaskPage
    {
        public NewTaskPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
            : base(driver, wait, urlsConfig.DbUpdateNewTaskPageUrl)
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