using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class LandingPageSteps
    {
        private readonly LandingPage _landingPage;

        public LandingPageSteps(IWebDriver driver)
        {
            _landingPage = new LandingPage(driver, 5);
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
