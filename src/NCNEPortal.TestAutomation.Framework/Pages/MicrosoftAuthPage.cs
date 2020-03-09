using System;
using NCNEPortal.TestAutomation.Framework.Configs;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace NCNEPortal.TestAutomation.Framework.Pages
{
    public class MicrosoftAuthPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;
        private readonly SecretsConfig _secretsConfig;

        public MicrosoftAuthPage(IWebDriver driver, WebDriverWait wait, SecretsConfig secretsConfig)
        {
            _driver = driver;
            _wait = wait;
            _secretsConfig = secretsConfig;
        }

        public bool HasLoaded
        {
            get
            {
                var isMicrosoftAuthPage = false;
                var shortWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5))
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
        
        private IWebElement UsernameField => _driver.FindElement(By.Id("i0116"));
        private IWebElement PasswordField => _driver.FindElement(By.Id("i0118"));
        
        public void Login()
        {
            var username = $"{_secretsConfig.LoginAccount}@ukho.gov.uk";
            EnterTextInField(username, UsernameField);
            UsernameField.SendKeys(Keys.Enter);

            var password = _secretsConfig.LoginPassword;
            EnterTextInField(password, PasswordField);
            PasswordField.Submit();
        }

        private void EnterTextInField(string textToEnter, IWebElement element)
        {
            _wait.Until(d =>
            {
                try
                {
                    return element.Displayed;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            });

            element.Clear();
            element.SendKeys(textToEnter);
        }
    }
}