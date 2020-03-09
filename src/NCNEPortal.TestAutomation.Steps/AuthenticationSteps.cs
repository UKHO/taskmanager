using NCNEPortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Steps
{
    [Binding]
    public class AuthenticationSteps
    {
        private readonly MicrosoftAuthPage _microsoftAuthPage;

        public AuthenticationSteps(MicrosoftAuthPage microsoftAuthPage)
        {
            _microsoftAuthPage = microsoftAuthPage;
        }

        [Given(@"I am an unauthenticated user")]
        public void GivenIAmAnUnauthenticatedUser()
        {
            //No op. By default I am unauthenticated
        }

        [When(@"I log in")]
        public void WhenILogIn()
        {
            _microsoftAuthPage.Login();
        }

        [Then(@"I am redirected to the login page")]
        public void ThenIAmRedirectedToTheLoginPage()
        {
            Assert.IsTrue(_microsoftAuthPage.HasLoaded);
        }
    }
}