using Common.Helpers;
using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Steps
{ 
    [Binding]
    public class ReviewPageSteps
    {
        private readonly ReviewPage _reviewPage;

        public ReviewPageSteps(IWebDriver driver, WorkflowDbContext workflowDbContext)
        {
            TestWorkflowDatabaseSeeder.UsingDbContext(workflowDbContext).PopulateTables().SaveChanges();

            _reviewPage = new ReviewPage(driver, 5);
        }

        [Given(@"I navigate to the review page")]
        public void GivenINavigateToTheReviewPage()
        {
            _reviewPage.NavigateTo();
        }

        [Then(@"The review page has loaded")]
        public void ThenTheReviewPageHasLoaded()
        {
            _reviewPage.HasLoaded();
        }
    }
}
