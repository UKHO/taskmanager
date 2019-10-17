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
            //TestWorkflowDatabaseSeeder.UsingDbContext(workflowDbContext).PopulateTables().SaveChanges();

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

        [When(@"I click an assessment")]
        public void WhenIClickAnAssessment()
        {
            ScenarioContext.Current.Pending();
        }

        //[Then(@"process ID on the screen has a unique entry in the database")]
        //public void ThenProcessIDOnTheScreenHasAUniqueEntryInTheDatabase()
        //{
        //    ScenarioContext.Current.Pending();
        //}

        //[When(@"I expand the source document details")]
        //public void WhenIExpandTheSourceDocumentDetails()
        //{
        //    ScenarioContext.Current.Pending();
        //}

        //[Then(@"the linked documents are displayed on the screen")]
        //public void ThenTheLinkedDocumentsAreDisplayedOnTheScreen()
        //{
        //    ScenarioContext.Current.Pending();
        //}

        //[Then(@"the linked documents displayed on the screen are the same as in the database")]
        //public void ThenTheLinkedDocumentsDisplayedOnTheScreenAreTheSameAsInTheDatabase()
        //{
        //    ScenarioContext.Current.Pending();
        //}

    }
}
