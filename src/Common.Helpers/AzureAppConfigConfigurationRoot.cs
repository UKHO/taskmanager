using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Common.Helpers
{
    public static class AzureAppConfigConfigurationRoot
    {
        private static IConfigurationRoot _instance;

        public static IConfigurationRoot Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigurationBuilder()
                        .AddAzureAppConfiguration(Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING")).Build();
                }

                return _instance;
            }
        }
    }
}