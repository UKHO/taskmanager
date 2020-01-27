using NCNEPortal.TestAutomation.Framework;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Steps
{
    [Binding]
    public sealed class WorkflowPageSteps
    {
        [Given(@"I navagate to the NCNE Workflow page")]
        public void GivenINavagateToTheNCNEWorkflowPage()
        {
            ScenarioContext.Current.Pending();
        }

        [Then(@"The NCNE Workflow page has loaded")]
        public void ThenTheNCNEWorkflowPageHasLoaded()
        {
            ScenarioContext.Current.Pending();
        }

    }
}
