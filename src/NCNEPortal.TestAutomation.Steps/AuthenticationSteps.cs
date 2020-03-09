using NCNEPortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Steps
{
    [Binding]
    public class AuthenticationSteps
    {
        private readonly LandingPageSteps _landingPageSteps;
        private readonly MicrosoftAuthPage _microsoftAuthPage;

        public AuthenticationSteps(MicrosoftAuthPage microsoftAuthPage, LandingPageSteps landingPageSteps)
        {
            _microsoftAuthPage = microsoftAuthPage;
            _landingPageSteps = landingPageSteps;
        }

        [Given(@"I am an unauthenticated user")]
        [Scope(Tag = "skipLogin")]
        public void GivenIAmAnUnauthenticatedUser()
        {
            //No op as this can only be used with the @skipLogin tag
        }

        [Given(@"I am an authenticated user")]
        public void GivenIAmAnAuthenticatedUser()
        {
            //No op. By default I am authenticated through the BeforeScenario steps
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

        [Then(@"I am not prompted to log in")]
        public void ThenIAmNotPromptedToLogIn()
        {
            _landingPageSteps.ThenTheLandingPageHasLoaded();
        }
    }
}