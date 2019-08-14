using NUnit.Framework;
using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages.Examples;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Steps.Examples
{
    [Binding]
    public sealed class ExamplePageSteps
    {
        private readonly ScenarioContext _context;

        private readonly ExamplePage _examplePage;

        public ExamplePageSteps(ScenarioContext injectedContext, IWebDriver driver)
        {
            _context = injectedContext;
            _examplePage = new ExamplePage(driver, 10);
        }

        [When(@"I navigate to the landing page")]
        public void WhenINavigateToTheLandingPage()
        {
            _examplePage.NavigateTo();
            _examplePage.HasLoaded();
        }

        [Then(@"I can enter the username as '(.*)'")]
        public void ThenICanEnterTheUserName(string username)
        {
            _examplePage.EnterUsername(username);
            Assert.AreEqual(username, _examplePage.GetUsernameValue());
        }
    }
}
