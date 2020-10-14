using Common.TestAutomation.Framework.Pages;
using NCNEPortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;

namespace NCNEPortal.TestAutomation.Framework.Pages
{
    public class WithdrawalPage : PageBase, ILandingPage
    {
        public WithdrawalPage(IWebDriver driver, WebDriverWait wait, UrlsConfig urlsConfig) : base(driver, wait,
            urlsConfig.NcneWithdrawalPageUrl)
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
