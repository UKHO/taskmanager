using System;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Common.Helpers
{
    public static class SecretsHelpers
    {
        public static (string keyVaultAddress, KeyVaultClient keyVaultClient) SetUpKeyVaultClient()
        {
            var keyVaultAddress = Environment.GetEnvironmentVariable("KEY_VAULT_ADDRESS");
            var tokenProvider = new AzureServiceTokenProvider();

            return (keyVaultAddress, new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback)));
        }
    }
}
