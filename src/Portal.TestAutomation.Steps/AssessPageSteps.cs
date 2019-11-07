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
    public class AssessPageSteps
    {
        private readonly AssessPage _assessPage;
        private readonly WorkflowDbContext _workflowDbContext;
        private readonly WorkflowInstanceContext _workflowContext;
        private readonly IWebDriver _driver;

        public AssessPageSteps(IWebDriver driver, WorkflowDbContext workflowDbContext, WorkflowInstanceContext workflowContext)
        {
            _workflowDbContext = workflowDbContext;
            _workflowContext = workflowContext;
            //TestWorkflowDatabaseSeeder.UsingDbContext(_workflowDbContext).PopulateTables().SaveChanges();
            _assessPage = new AssessPage(driver, 5);
        }
                
        [Given(@"I navigate to the assess page")]
        public void GivenINavigateToTheAssessPage()
        {
            _assessPage.NavigateTo();
        }

        [Then(@"The assess page has loaded")]
        public void ThenTheAssessPageHasLoaded()
        {
            _assessPage.HasLoaded();
        }
    }
}
