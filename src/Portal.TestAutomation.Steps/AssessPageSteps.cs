using NUnit.Framework;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class AssessPageSteps
    {
        private readonly AssessPage _assessPage;

        public AssessPageSteps(AssessPage assessPage)
        {
            _assessPage = assessPage;
        }

        [Given(@"I navigate to the assess page")]
        public void GivenINavigateToTheAssessPage()
        {
            _assessPage.NavigateTo();
        }

        [Then(@"The assess page has loaded")]
        public void ThenTheAssessPageHasLoaded()
        {
            Assert.IsTrue(_assessPage.HasLoaded);
        }
    }
}