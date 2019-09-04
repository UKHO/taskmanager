using Common.Helpers;
using Microsoft.Extensions.Configuration;
using Portal.TestAutomation.Framework.Configuration;

namespace Portal.TestAutomation.Framework.Setup
{
    public static class SetupConfig
    {
        public static DbConfig GetAndBindDbConfig()
        {
            var config = new DbConfig();

            var configRoot = AzureAppConfigConfigurationRoot.Instance;
            configRoot.GetSection("databases").Bind(config);
            configRoot.GetSection("urls").Bind(config);

            return config;
        }

        public static SecretsConfig GetAndBindSecretsConfig()
        {
            var config = new SecretsConfig();

            var configRoot = AzureKeyVaultConfigConfigurationRoot.Instance;
            configRoot.GetSection("WorkflowDbSection").Bind(config);

            return config;
        }
    }
}
