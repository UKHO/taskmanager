using System.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Portal.TestAutomation.Framework.ContextClasses;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class ReviewPageSteps
    {
        private readonly ReviewPage _reviewPage;
        private readonly WorkflowInstanceContext _workflowContext;
        private readonly WorkflowDbContext _workflowDbContext;

        public ReviewPageSteps(IWebDriver driver, WorkflowDbContext workflowDbContext,
            WorkflowInstanceContext workflowContext, ReviewPage reviewPage)
        {
            _workflowDbContext = workflowDbContext;
            _workflowContext = workflowContext;
            _reviewPage = reviewPage;
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
            Assert.IsTrue(_reviewPage.HasLoaded);
        }

        [Then(@"The source document with the corresponding process Id in the database matches the sdocId on the UI")]
        public void ThenTheSourceDocumentWithTheCorrespondingProcessIdInTheDatabaseMatchesTheSdocIdOnTheUI()
        {
            var sDocId = _workflowDbContext.AssessmentData.First(x => x.ProcessId == _workflowContext.ProcessId)
                .PrimarySdocId;
            Assert.IsTrue(_reviewPage.IsSdocIdInDetails(sDocId));
        }
    }
}