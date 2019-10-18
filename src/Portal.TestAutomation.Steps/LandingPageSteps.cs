using Common.Helpers;
using NUnit.Framework;
using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class LandingPageSteps
    {
        private readonly LandingPage _landingPage;

        public LandingPageSteps(IWebDriver driver, WorkflowDbContext workflowDbContext)
        {
            TestWorkflowDatabaseSeeder.UsingDbContext(workflowDbContext).PopulateTables().SaveChanges();

            _landingPage = new LandingPage(driver, 5);
        }

        [Given(@"I navigate to the landing page")]
        public void GivenINavigateToTheLandingPage()
        {
            _landingPage.NavigateTo();
        }

        [Then(@"The landing page has loaded")]
        public void ThenTheLandingPageHasLoaded()
        {
            _landingPage.HasLoaded();
        }

        [When(@"I enter Process Id of ""(.*)""")]
        public void WhenIEnterProcessIdOf(int processId)
        {
            _landingPage.FilterRowsByProcessIdInGlobalSearch(processId);
        }

        [Then(@"Task with process id (.*) appears in both the assigned and unassigned tasks tables")]
        public void ThenTaskWithProcessIdAppearsInBothTheAssignedAndUnassignedTasksTables(int p0)
        {
            Assert.IsTrue(_landingPage.FindTaskByProcessId(p0));
        }

        [When(@"I click an assessment")]
        public void WhenIClickAnAssessment()
        {
            _landingPage.SelectAssessment();
        }
    }
}
