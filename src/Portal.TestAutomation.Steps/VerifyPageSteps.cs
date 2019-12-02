using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class VerifyPageSteps
    {
        private readonly VerifyPage _verifyPage;

        public VerifyPageSteps(IWebDriver driver)
        {
            //TestWorkflowDatabaseSeeder.UsingDbContext(_workflowDbContext).PopulateTables().SaveChanges();
            _verifyPage = new VerifyPage(driver, 5);
        }

        [Given(@"I navigate to the verify page")]
        public void GivenINavigateToTheVerifyPage()
        {
            _verifyPage.NavigateTo();
        }

        [Then(@"The verify page has loaded")]
        public void ThenTheVerifyPageHasLoaded()
        {
            _verifyPage.HasLoaded();
        }
    }
}
