using Common.Helpers;
using NUnit.Framework;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class LandingPageSteps
    {
        private readonly LandingPage _landingPage;

        public LandingPageSteps(WorkflowDbContext workflowDbContext, LandingPage landingPage)
        {
            _landingPage = landingPage;
        }

        [When(@"I navigate to the landing page")]
        public void WhenINavigateToTheLandingPage()
        {
            _landingPage.NavigateTo();
        }

        [When(@"I enter Process Id of ""(.*)""")]
        public void WhenIEnterProcessIdOf(int processId)
        {
            _landingPage.FilterRowsByProcessIdInGlobalSearch(processId);
        }

        [Then(@"The landing page has loaded")]
        [Then(@"I am redirected to the landing page")]
        public void ThenTheLandingPageHasLoaded()
        {
            Assert.IsTrue(_landingPage.HasLoaded);
        }

        [Then(@"Task with process id (.*) appears in both the assigned and unassigned tasks tables")]
        public void ThenTaskWithProcessIdAppearsInBothTheAssignedAndUnassignedTasksTables(int p0)
        {
            Assert.IsTrue(_landingPage.FindTaskByProcessId(p0));
        }
    }
}