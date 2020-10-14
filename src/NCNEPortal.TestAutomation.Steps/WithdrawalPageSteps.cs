using NCNEPortal.TestAutomation.Framework.Pages;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Steps
{
    [Binding]
    public sealed class WithdrawalPageSteps
    {

        private readonly WithdrawalPage _withdrawalPage;
        public WithdrawalPageSteps(WithdrawalPage withdrawalPage)

        {
            _withdrawalPage = withdrawalPage;
        }


        [Given(@"I navigate to the NCNE Withdrawal Task page")]
        public void GivenINavigateToTheNCNEWithdrawalPage()

        {
            _withdrawalPage.NavigateTo();
        }


        [Then(@"The NCNE Withdrawal Task page has loaded")]
        public void ThenTheNCNEWithdrawalPageHasLoaded()

        {
            Assert.IsTrue(_withdrawalPage.HasLoaded);
        }
    }
}
