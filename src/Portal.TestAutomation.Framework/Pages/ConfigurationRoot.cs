using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System;

namespace Portal.TestAutomation.Framework.Pages
{
    public static class ConfigurationRoot
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