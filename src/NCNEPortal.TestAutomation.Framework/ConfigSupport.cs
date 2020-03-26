using BoDi;
using Common.Helpers;
using Common.TestAutomation.Framework.Configs;
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
        private readonly IObjectContainer _objectContainer;

        public ConfigSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 0)]
        public void RegisterConfigs()
        {
            var appConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultRoot = AzureKeyVaultConfigConfigurationRoot.Instance;

            var secrets = new SecretsConfig();
            var urls = new UrlsConfig();

            appConfigRoot.GetSection("urls").Bind(urls);
            keyVaultRoot.GetSection("NCNEPortalUITest").Bind(secrets);

            _objectContainer.RegisterInstanceAs(secrets);
            _objectContainer.RegisterInstanceAs(urls);
        }

        [BeforeScenario(Order = 3)]
        public void RegisterLandingPage()
        {
            _objectContainer.RegisterTypeAs<LandingPage, ILandingPage>();
        }
    }
}