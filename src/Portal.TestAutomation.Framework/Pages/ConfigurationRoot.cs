using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System;

namespace Portal.TestAutomation.Framework.Pages
{
    public sealed class ConfigurationRoot
    {
        private static IConfigurationRoot _instance = null;

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