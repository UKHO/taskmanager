using NCNEPortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Steps
{

    [Binding]
    public sealed class NewTaskPageSteps

    {
        private readonly NewTaskPage _newtaskPage;
        public NewTaskPageSteps(NewTaskPage newtaskPage)

        {
            _newtaskPage = newtaskPage;
        }

        
        [Given(@"I navigate to the NCNE New Task page")]
        public void GivenINavigateToTheNCNENewTaskPage()

        {
            _newtaskPage.NavigateTo();
        }

        
        [Then(@"The NCNE New Task page has loaded")]
        public void ThenTheNCNENewTaskPageHasLoaded()

        {
            Assert.IsTrue(_newtaskPage.HasLoaded());
        }
    }


}
