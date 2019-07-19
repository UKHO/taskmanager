using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Portal.TestAutomation.Framework.Pages
{
    public class ExamplePage : BasePage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        #region ExampleWebElements

        private By UsernameLocator => By.Id("user-name");
        private IWebElement UsernameField => _driver.FindElement(UsernameLocator);

        private By PasswordLocator => By.Id("user-name");
        private IWebElement PasswordField => _driver.FindElement(PasswordLocator);

        #endregion

        public ExamplePage(IWebDriver driver, int seconds)
        {
            _driver = driver;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(seconds));
        }

        public override bool HasLoaded()
        {
            try
            {
                _wait.Until(driver => UsernameField.Displayed);
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
            _driver.Navigate().GoToUrl("https://www.saucedemo.com/");
        }

        public void EnterUsername(string username)
        {
            UsernameField.Click();
            UsernameField.SendKeys(username);
        }

        public string GetUsernameValue()
        {
            return UsernameField.GetAttribute("value");
        }
    }
}
