using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NCNEPortal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureAppConfiguration(builder =>
                {
                    var azureAppConfConnectionString =
                        Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                    var (keyVaultAddress, keyVaultClient) = SecretsHelpers.SetUpKeyVaultClient();

                    builder.AddAzureAppConfiguration(azureAppConfConnectionString)
                        .AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager()).Build();
                });
    }
}
