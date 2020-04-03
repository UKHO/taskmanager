using System.Linq;
using NUnit.Framework;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class ReviewPageSteps
    {
        private readonly ReviewPage _reviewPage;
        private readonly WorkflowDbContext _workflowDbContext;

        private int _loadedTaskProcessId;

        public ReviewPageSteps(WorkflowDbContext workflowDbContext, ReviewPage reviewPage)
        {
            _workflowDbContext = workflowDbContext;
            _reviewPage = reviewPage;
        }

        [When(@"I go to the review page for a task")]
        public void WhenIGoToTheReviewPageForATask()
        {
            _loadedTaskProcessId = _workflowDbContext.WorkflowInstance
                .First(w => w.ActivityName == "Review").ProcessId;

            _reviewPage.NavigateToProcessId(_loadedTaskProcessId);

            Assume.That(_reviewPage.HasLoaded);
        }

        [Then(@"I can see the primary source document for that task")]
        public void ThenICanSeeThePrimarySourceDocumentForThatTask()
        {
            var expectedPrimarySdocId = _workflowDbContext.AssessmentData
                .First(x => x.ProcessId == _loadedTaskProcessId)
                .PrimarySdocId;

            Assert.IsTrue(_reviewPage.IsSdocIdInDetails(expectedPrimarySdocId));
        }
    }
}