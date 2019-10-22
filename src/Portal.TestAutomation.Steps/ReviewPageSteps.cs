using NUnit.Framework;
using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;
using System.Linq;
using Portal.TestAutomation.Framework.ContextClasses;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class ReviewPageSteps
    {
        private readonly ReviewPage _reviewPage;
        private readonly WorkflowDbContext _workflowDbContext;
        private readonly WorkflowInstanceContext _workflowContext;
        private readonly IWebDriver _driver;

        public ReviewPageSteps(IWebDriver driver, WorkflowDbContext workflowDbContext, WorkflowInstanceContext workflowContext)
        {
            _workflowDbContext = workflowDbContext;
            _workflowContext = workflowContext;
            //TestWorkflowDatabaseSeeder.UsingDbContext(_workflowDbContext).PopulateTables().SaveChanges();
            _reviewPage = new ReviewPage(driver, 5);
        }

        [Given(@"I navigate to the review page")]
        public void GivenINavigateToTheReviewPage()
        {
            _reviewPage.NavigateTo();
        }

        [Given(@"The review page has loaded with the first process Id")]
        public void GivenTheReviewPageHasLoadedWithTheFirstProcessId()
        {
            var firstProcessId = _workflowDbContext.WorkflowInstance.First().ProcessId;
            _workflowContext.ProcessId = firstProcessId;
            _reviewPage.NavigateToProcessId(firstProcessId);
        }

        [Then(@"The review page has loaded")]
        public void ThenTheReviewPageHasLoaded()
        {
            _reviewPage.HasLoaded();
        }

       [Then(@"The source document with the corresponding process Id in the database matches the sdocId on the UI")]
        public void ThenTheSourceDocumentWithTheCorrespondingProcessIdInTheDatabaseMatchesTheSdocIdOnTheUI()
        {
            var sdocId = _workflowDbContext.AssessmentData.First(x => x.ProcessId == _workflowContext.ProcessId).SdocId;
            var firstUiDisplayDoc = _driver.FindElement(By.XPath($"/html/body//table/tbody/tr/td[contains(text(),{sdocId})]"));
            Assert.AreSame(sdocId, firstUiDisplayDoc);
        }
    }
}


