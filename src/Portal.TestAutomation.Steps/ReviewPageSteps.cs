using System.Runtime.CompilerServices;
using Common.Helpers;
using NUnit.Framework;
using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;
using System.Linq;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class ReviewPageSteps
    {
        private readonly ReviewPage _reviewPage;
        private readonly WorkflowDbContext workflowDbContext;

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

        [When(@"I expand the source document details")]
        public void WhenIExpandTheSourceDocumentDetails()
        {
            _reviewPage.ExpandSourceDocumentDetails();
        }

        [Then(@"the linked documents are displayed on the screen")]
        public void ThenTheLinkedDocumentsAreDisplayedOnTheScreen()
        {
            Assert.IsTrue(_reviewPage.SourceDocumentRowCount() > 1);
        }

        [Then(@"the linked documents displayed on the screen are the same as in the database")]
        public void ThenTheLinkedDocumentsDisplayedOnTheScreenAreTheSameAsInTheDatabase()
        {
          var firstProcessId =  workflowDbContext.WorkflowInstance.First().ProcessId;
          var sdocId = workflowDbContext.AssessmentData.First(x => x.ProcessId == firstProcessId).SdocId;
          var sourcedocs = workflowDbContext.SourceDocumentStatus.Where(x => x.ProcessId == firstProcessId).ToList();

          var d = sourcedocs.First(x => x.SdocId == sdocId);
        }
    }
}


