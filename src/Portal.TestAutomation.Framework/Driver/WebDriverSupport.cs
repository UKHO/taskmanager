using BoDi;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Framework.Driver
{
    [Binding]
    internal sealed class WebDriverSupport
    {
        private readonly IObjectContainer _objectContainer;

        public WebDriverSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 0)]
        public void InitializeWebDriver()
        {
            var webDriver = new ChromeDriver();
            _objectContainer.RegisterInstanceAs<IWebDriver>(webDriver);
        }
    }
}
