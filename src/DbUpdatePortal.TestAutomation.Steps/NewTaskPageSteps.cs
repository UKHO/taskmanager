using DbUpdatePortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace DbUpdatePortal.TestAutomation.Steps
{
    [Binding]
    public class NewTaskPageSteps
    {
        private readonly NewTaskPage _newtaskPage;

        public NewTaskPageSteps(NewTaskPage newTaskPage)
        {
            _newtaskPage = newTaskPage;
        }

        [Given(@"I navigate to the Db Update New Task page")]
        public void GivenINavigateToTheDbUpdateNewTaskPage()
        {
            _newtaskPage.NavigateTo();
        }

        [Then(@"The Db Update New Task page has loaded")]
        public void ThenTheDbUpdateNewTaskPageHasLoaded()
        {
            Assert.IsTrue(_newtaskPage.HasLoaded);
        }

    }
}
