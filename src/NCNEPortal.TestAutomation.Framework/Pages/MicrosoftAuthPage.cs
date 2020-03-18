using System;
using NCNEPortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework.Pages
{
    public class MicrosoftAuthPage : PageBase
    {
        private readonly LandingPage _landingPage;
        private readonly SecretsConfig _secretsConfig;

        public MicrosoftAuthPage(IWebDriver driver, WebDriverWait wait, SecretsConfig secretsConfig,
            LandingPage landingPage) : base(driver, wait, null)
        {
            _secretsConfig = secretsConfig;
            _landingPage = landingPage;
        }

        private IWebElement UsernameField => Driver.FindElement(By.Id("i0116"));
        private IWebElement PasswordField => Driver.FindElement(By.Id("i0118"));

        public override bool HasLoaded
        {
            get
            {
                var isMicrosoftAuthPage = false;
                var shortWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(5))
                {
                    PollingInterval = TimeSpan.FromMilliseconds(100)
                };

                shortWait.Until(d =>
                {
                    try
                    {
                        if (UsernameField.Displayed)
                        {
                            isMicrosoftAuthPage = true;
                            return true;
                        }
                    }
                    catch (NoSuchElementException)
                    {
                        if (!d.Url.Contains("login.microsoftonline.com"))
                        {
                            isMicrosoftAuthPage = false;
                            return true;
                        }
                    }

                    return false;
                });

                return isMicrosoftAuthPage;
            }
        }

        public override void NavigateTo()
        {
            //Going to the landing page when not logged in will redirect to the auth page
            _landingPage.NavigateTo();
        }

        public void Login()
        {
            var username = $"{_secretsConfig.LoginAccount}@ukho.gov.uk";
            EnterTextInField(username, () => UsernameField);
            UsernameField.SendKeys(Keys.Enter);

            var password = _secretsConfig.LoginPassword;
            EnterTextInField(password, () => PasswordField);
            PasswordField.Submit();

            CookieStore.SaveCookies(Driver.Manage().Cookies);
        }
    }
}