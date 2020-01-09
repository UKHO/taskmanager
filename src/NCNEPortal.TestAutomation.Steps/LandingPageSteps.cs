using NCNEPortal.TestAutomation.Framework;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Steps
{
    [Binding]
    public sealed class LandingPageSteps
    {
        private readonly LandingPage _landingPage;

        public LandingPageSteps(LandingPage landingPage)
        {
            _landingPage = landingPage;
        }

        [Given(@"I navigate to the landing page")]
        public void GivenINavigateToTheLandingPage()
        {
            _landingPage.NavigateTo();
        }

        [Then(@"The landing page has loaded")]
        public void ThenTheLandingPageHasLoaded()
        {
            _landingPage.HasLoaded();
        }
    }
}