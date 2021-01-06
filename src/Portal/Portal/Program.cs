using System;
using System.Collections.Generic;
using Common.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Portal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseEnvironment(ConfigHelpers.HostBuilderEnvironment)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureAppConfiguration(builder =>
                {
                    var azureAppConfConnectionString = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                    var (keyVaultAddress, keyVaultClient) = SecretsHelpers.SetUpKeyVaultClient();

                    builder.AddAzureAppConfiguration(azureAppConfConnectionString)
                    .AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager()).Build();
                });
    }
}
