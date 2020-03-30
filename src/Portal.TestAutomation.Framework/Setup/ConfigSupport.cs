using BoDi;
using Common.Helpers;
using Common.TestAutomation.Framework.Pages;
using Microsoft.Extensions.Configuration;
using Portal.TestAutomation.Framework.Configuration;
using Portal.TestAutomation.Framework.Pages;
using TechTalk.SpecFlow;

namespace Portal.TestAutomation.Framework.Setup
{
    [Binding]
    public class ConfigSupport
    {
        private readonly IObjectContainer _objectContainer;

        public ConfigSupport(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario(Order = 1)]
        public void RegisterConfigs()
        {
            var appConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultRoot = AzureKeyVaultConfigConfigurationRoot.Instance;

            var secrets = new SecretsConfig();
            var urls = new UrlsConfig();
            var dbConfig = new DbConfig();

            appConfigRoot.GetSection("databases").Bind(dbConfig);
            appConfigRoot.GetSection("urls").Bind(dbConfig);
            appConfigRoot.GetSection("urls").Bind(urls);

            keyVaultRoot.GetSection("PortalUITest").Bind(secrets);
            keyVaultRoot.GetSection("WorkflowDbSection").Bind(secrets);


            _objectContainer.RegisterInstanceAs((Common.TestAutomation.Framework.Configs.SecretsConfig)secrets);
            _objectContainer.RegisterInstanceAs(secrets);
            _objectContainer.RegisterInstanceAs(urls);
            _objectContainer.RegisterInstanceAs(dbConfig);
        }

        [BeforeScenario(Order = 19)]
        public void RegisterLandingPage()
        {
            _objectContainer.RegisterTypeAs<LandingPage, ILandingPage>();
        }
    }
}