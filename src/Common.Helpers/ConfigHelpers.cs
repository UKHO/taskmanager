using System;

namespace Common.Helpers
{
    public static class ConfigHelpers
    {
        private static string EnvironmentName => Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "";

        public static bool IsLocalDevelopment => EnvironmentName.Equals("LocalDevelopment", StringComparison.OrdinalIgnoreCase);

        public static bool IsAzureDevOpsBuild => EnvironmentName.Equals("AzureDevOpsBuild", StringComparison.OrdinalIgnoreCase);
        public static bool IsAzureDevelopment => EnvironmentName.Equals("AzureDevelopment", StringComparison.OrdinalIgnoreCase);
        public static bool IsAzureUat => EnvironmentName.Equals("AzureUAT", StringComparison.OrdinalIgnoreCase);

        public static bool IsAzure =>
            EnvironmentName.Equals("Azure", StringComparison.OrdinalIgnoreCase);
    }
}
