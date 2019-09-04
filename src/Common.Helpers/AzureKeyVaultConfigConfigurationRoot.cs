using System;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace Common.Helpers
{
    public static class AzureKeyVaultConfigConfigurationRoot
    {
        private static IConfigurationRoot _instance;

        public static IConfigurationRoot Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var keyVaultAddress = Environment.GetEnvironmentVariable("KEY_VAULT_ADDRESS");
                if (string.IsNullOrEmpty(keyVaultAddress)) throw new ApplicationException("Missing environment variable: KEY_VAULT_ADDRESS");

                var tokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient =
                    new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

                _instance = new ConfigurationBuilder().AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager()).Build();

                return _instance;
            }
        }
    }
}