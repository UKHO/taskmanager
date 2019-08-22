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
        private readonly WorkflowDbContext _workflowDbContext;
        private readonly LandingPage _landingPage;

        public LandingPageSteps(IWebDriver driver, WorkflowDbContext workflowDbContext)
        {
            _workflowDbContext = workflowDbContext;

            TasksDbBuilder.UsingDbContext(_workflowDbContext).PopulateTables().SaveChanges();

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

        [Then(@"Task with process id (.*) appears")]
        public void ThenTaskWithProcessIdAppears(int p0)
        {
            var row = _landingPage.FindTaskByProcessId(p0);

            if (row == null) Assert.Fail();
            else Assert.Pass();
        }


    }
}
