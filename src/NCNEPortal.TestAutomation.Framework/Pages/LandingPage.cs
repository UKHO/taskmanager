using System;
using Common.TestAutomation.Framework.Pages;
using NCNEPortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework.Pages
{
    public class LandingPage : PageBase, ILandingPage
    {
        public LandingPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig)
            : base(driver, wait, urlsConfig.NcneLandingPageUrl)
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