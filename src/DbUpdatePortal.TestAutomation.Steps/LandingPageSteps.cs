using DbUpdatePortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace DbUpdatePortal.TestAutomation.Steps
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
            Assert.IsTrue(_landingPage.HasLoaded);
        }
    }
}