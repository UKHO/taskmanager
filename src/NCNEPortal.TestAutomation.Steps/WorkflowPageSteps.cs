using NCNEPortal.TestAutomation.Framework;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Steps
{
    [Binding]
    public sealed class WorkflowPageSteps
    {

        private readonly WorkflowPage _workflowPage;
        public WorkflowPageSteps(WorkflowPage workflowPage)
        {
            _workflowPage = workflowPage;
        }

        [Given(@"I navagate to the NCNE Workflow page")]
        public void GivenINavagateToTheNCNEWorkflowPage()
        {
            _workflowPage.NavigateTo();
        }

        [Then(@"The NCNE Workflow page has loaded")]
        public void ThenTheNCNEWorkflowPageHasLoaded()
        {
            _workflowPage.HasLoaded();
        }

    }
}
