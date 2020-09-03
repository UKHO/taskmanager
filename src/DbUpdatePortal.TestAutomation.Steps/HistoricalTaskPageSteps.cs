using DbUpdatePortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace DbUpdatePortal.TestAutomation.Steps
{
    [Binding]
    public class HistoricalTaskPageSteps
    {
        private readonly HistoricalTaskPage _historicalTaskPage;

        public HistoricalTaskPageSteps(HistoricalTaskPage historicalTaskPage)
        {
            _historicalTaskPage = historicalTaskPage;
        }

        [Given(@"I navigate to the Db Update Historical Tasks page")]
        public void GivenINavigateToTheDbUpdateHistoricalTasksPage()
        {
            _historicalTaskPage.NavigateTo(); 
        }

        [Then(@"The Db Update Historical Tasks page has loaded")]
        public void ThenTheDbUpdateHistoricalTasksPageHasLoaded()
        {
            Assert.IsTrue(_historicalTaskPage.HasLoaded);
        }

    }
}
