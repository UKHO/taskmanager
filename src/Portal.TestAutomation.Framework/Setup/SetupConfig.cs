using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Azure.KeyVault;
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

        public static async Task<SecretsConfig> GetAndBindSecretsConfig(string address, KeyVaultClient kvClient)
        {
            var config = new SecretsConfig();

            // Get the sql acct pwd from Key Vault
            var pwd = await kvClient.GetSecretAsync(address + "secrets/WorkflowDbSection--SqlAcctPwd").ConfigureAwait(false);
            config.WorkflowDbPassword = pwd.Value;
            return config;
        }
    }
}
