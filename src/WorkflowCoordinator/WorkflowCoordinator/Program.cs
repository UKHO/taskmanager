using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.ObjectBuilder.MSDependencyInjection;
using System;
using System.Threading.Tasks;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;

namespace WorkflowCoordinator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
            .UseEnvironment(Environment.GetEnvironmentVariable("ENVIRONMENT"))
            .ConfigureWebJobs((context, b) =>
            {
                b.AddAzureStorageCoreServices();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var keyVaultAddress = Environment.GetEnvironmentVariable("KEY_VAULT_ADDRESS");

                var tokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

                var azureAppConfConnectionString = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                config.AddAzureAppConfiguration(new AzureAppConfigurationOptions()
                {
                    ConnectionString = azureAppConfConnectionString
                });

                config
                    //.SetBasePath(Directory.GetCurrentDirectory())
                    //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    //.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    //.AddEnvironmentVariables()
                    .AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager()).Build();
            })
            .ConfigureLogging((hostingContext, b) =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices((hostingContext, services) =>
            {
                var startupConfig = new StartupConfig();
                hostingContext.Configuration.GetSection("urls").Bind(startupConfig);
                hostingContext.Configuration.GetSection("workflowcoordinator").Bind(startupConfig);

                var endpointConfiguration = new EndpointConfiguration(startupConfig.EndpointName);
                services.AddOptions<UrlsConfig>()
                    .Configure(o => o.BaseUrl = startupConfig.DataAccessWebServiceLocalhostBaseUri.ToString());
                services.AddSingleton<EndpointConfiguration>(endpointConfiguration);
                services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

                UpdateableServiceProvider container = null;

                endpointConfiguration.UseContainer<ServicesBuilder>(customizations =>
                {
                    customizations.ExistingServices(services);
                    customizations.ServiceProviderFactory(sc =>
                    {
                        container = new UpdateableServiceProvider(sc);
                        return container;
                    });
                });

                services.AddOptions<GeneralConfig>()
                    .Bind(hostingContext.Configuration.GetSection("workflowcoordinator"));

                services.AddOptions<SecretsConfig>()
                    .Bind(hostingContext.Configuration.GetSection("NsbDbSection"));

                services.AddScoped<IJobHost, NServiceBusJobHost>();
            })
            .UseConsoleLifetime();

            var host = builder.Build();

            using (host)
            {
                await host.RunAsync();
            }
        }
    }

    public class UrlsConfig
    {
        public string BaseUrl { get; set; }
    }
}