using BoDi;
using OpenQA.Selenium;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Framework
{
    [Binding]
    public class RegisterPom
    {
        private readonly IObjectContainer _objectContainer;

        public RegisterPom(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario]
        public void RegisterLandingPage()
        {
            var webDriver = _objectContainer.Resolve<IWebDriver>();
            _objectContainer.RegisterInstanceAs(new LandingPage(webDriver));
        }
    }
}