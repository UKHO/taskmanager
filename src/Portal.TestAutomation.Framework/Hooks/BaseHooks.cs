using OpenQA.Selenium;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Framework.Hooks
{
    [Binding]
    internal sealed class BaseHooks
    {
        private readonly IWebDriver _driver;

        public BaseHooks(IWebDriver driver)
        {
            _driver = driver;
        }

        [AfterScenario()]
        public void CloseBrowser()
        {
            _driver.Quit();
        }
    }
}
