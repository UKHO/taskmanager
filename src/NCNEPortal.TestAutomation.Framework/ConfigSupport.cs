using BoDi;
using Common.Helpers;
using Common.TestAutomation.Framework.Configs;
using Common.TestAutomation.Framework.Logging;
using Common.TestAutomation.Framework.Pages;
using Microsoft.Extensions.Configuration;
using NCNEPortal.TestAutomation.Framework.Configs;
using NCNEPortal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;

namespace NCNEPortal.TestAutomation.Framework
{
    [Binding]
    public class ConfigSupport
    {
        private static SecretsConfig _secrets;
        private static UrlsConfig _urls;
        private readonly IObjectContainer _objectContainer;

        public ConfigSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeTestRun(Order = 1)]
        public static void PopulateConfigsFromAzure()
        {
            var appConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultRoot = AzureKeyVaultConfigConfigurationRoot.Instance;

            _secrets = new SecretsConfig();
            _urls = new UrlsConfig();

            appConfigRoot.GetSection("urls").Bind(_urls);
            keyVaultRoot.GetSection("NCNEPortalUITest").Bind(_secrets);
        }

        [BeforeScenario(Order = 1)]
        public void RegisterAzureConfigs()
        {
            _objectContainer.RegisterInstanceAs(_secrets);
            _objectContainer.RegisterInstanceAs(_urls);
        }

        [BeforeScenario(Order = 5)]
        public void RegisterSpecFlowLogger()
        {
            _objectContainer.RegisterTypeAs<SpecFlowLogging, ITestLogging>();
        }

        [BeforeScenario(Order = 19)]
        public void RegisterLandingPage()
        {
            _objectContainer.RegisterTypeAs<LandingPage, ILandingPage>();
        }

        [BeforeScenario(Order = 19)]
        public void RegisterWorkflowPage()
        {
            _objectContainer.RegisterTypeAs<WorkflowPage, IWorkflowPage>();
        }

        [BeforeScenario(Order = 19)]
        public void RegisterWithdrawalPage()
        {
            _objectContainer.RegisterTypeAs<WithdrawalPage, ILandingPage>();
        }

        [BeforeScenario(Order = 19)]
        public void RegisterNewTaskPage()
        {
            _objectContainer.RegisterTypeAs<NewTaskPage, ILandingPage>();
        }
    }
}