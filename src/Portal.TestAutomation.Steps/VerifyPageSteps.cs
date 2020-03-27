using NUnit.Framework;
using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class VerifyPageSteps
    {
        private readonly VerifyPage _verifyPage;

        public VerifyPageSteps(VerifyPage verifyPage)
        {
            _verifyPage = verifyPage;
        }

        [Given(@"I navigate to the verify page")]
        public void GivenINavigateToTheVerifyPage()
        {
            _verifyPage.NavigateTo();
        }

        [Then(@"The verify page has loaded")]
        public void ThenTheVerifyPageHasLoaded()
        {
            Assert.IsTrue(_verifyPage.HasLoaded);
        }
    }
}
