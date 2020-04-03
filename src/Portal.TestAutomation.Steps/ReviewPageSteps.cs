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

        [Given(@"The review page has loaded with the first process Id")]
        public void GivenTheReviewPageHasLoadedWithTheFirstProcessId()
        {
            _loadedTaskProcessId = _workflowDbContext.WorkflowInstance
                .First(w => w.ActivityName=="Review").ProcessId;

            _reviewPage.NavigateToProcessId(_loadedTaskProcessId);

            Assume.That(_reviewPage.HasLoaded);
        }


        [Then(@"The source document with the corresponding process Id in the database matches the sdocId on the UI")]
        public void ThenTheSourceDocumentWithTheCorrespondingProcessIdInTheDatabaseMatchesTheSdocIdOnTheUI()
        {
            var expectedSDocId = _workflowDbContext.AssessmentData
                .First(x => x.ProcessId == _loadedTaskProcessId)
                .PrimarySdocId;

            Assert.IsTrue(_reviewPage.IsSdocIdInDetails(expectedSDocId));
        }
    }
}