using System.Linq;
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

        [Given(@"I am on the landing page")]
        [When(@"I navigate to the landing page")]
        public void WhenINavigateToTheLandingPage()
        {
            _landingPage.NavigateTo();
        }

        [Then(@"The landing page has loaded")]
        [Then(@"I am redirected to the landing page")]
        public void ThenTheLandingPageHasLoaded()
        {
            Assert.IsTrue(_landingPage.HasLoaded);
        }

        [Then(@"I should see all of the tasks assigned to me")]
        public void ThenIShouldSeeAllOfTheTasksAssignedToMe()
        {
            var inFlightTasks = _landingPage.InFlightTasks;

            Assert.That(inFlightTasks, Is.Not.Null.And.Count.AtLeast(1), "No inflight tasks found");
            Assert.That(inFlightTasks.Where(i => i.Stage == WorkflowStage.Review.ToString()), Has.All.Property("Reviewer").EqualTo(_landingPage.UserName));
            //Assert.That(inFlightTasks.Where(i => i.Stage == WorkflowStage.Assess.ToString()), Has.All.Property("Assessor").EqualTo(_landingPage.UserName));
            Assert.That(inFlightTasks.Where(i => i.Stage == WorkflowStage.Verify.ToString()), Has.All.Property("Verifier").EqualTo(_landingPage.UserName));
        }
    }
}