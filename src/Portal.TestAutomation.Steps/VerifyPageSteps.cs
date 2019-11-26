using NUnit.Framework;
using OpenQA.Selenium;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;
using WorkflowDatabase.EF;
using System.Linq;
using Portal.TestAutomation.Framework.ContextClasses;


namespace Portal.TestAutomation.Steps
{
    [Binding]
    public class VerifyPageSteps

    {
        private readonly VerifyPage verifyPage;
        private readonly WorkflowDbContext _workflowDbContext;
        private readonly WorkflowInstanceContext _workflowContext;
        private readonly IWebDriver _driver;

        public VerifyPageSteps(IWebDriver driver, WorkflowDbContext workflowDbContext, WorkflowInstanceContext workflowContext)
        {
            _workflowDbContext = workflowDbContext;
            _workflowContext = workflowContext;
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
