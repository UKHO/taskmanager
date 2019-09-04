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
                if (_instance != null) return _instance;

                var appConfigConnection = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");
                if (string.IsNullOrEmpty(appConfigConnection)) throw new ApplicationException("Missing environment variable: AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                _instance = new ConfigurationBuilder().AddAzureAppConfiguration(appConfigConnection).Build();

                return _instance;
            }
        }
    }

}