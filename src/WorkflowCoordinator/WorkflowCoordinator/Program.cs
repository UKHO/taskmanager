using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using NServiceBus.ObjectBuilder.MSDependencyInjection;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowDatabase.EF;

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
                var azureAppConfConnectionString = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                var tokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

                config.AddAzureAppConfiguration(new AzureAppConfigurationOptions()
                {
                    ConnectionString = azureAppConfConnectionString
                });

                config.AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager()).Build();
            })
            .ConfigureLogging((hostingContext, b) =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices((hostingContext, services) =>
            {
                var startupConfig = new StartupConfig();
                hostingContext.Configuration.GetSection("nsb").Bind(startupConfig);

                var endpointConfiguration = new EndpointConfiguration(startupConfig.WorkflowCoordinatorName);

                services.AddSingleton<EndpointConfiguration>(endpointConfiguration);
                services.AddOptions<GeneralConfig>()
                    .Bind(hostingContext.Configuration.GetSection("nsb"))
                    .Bind(hostingContext.Configuration.GetSection("urls"));

                services.AddOptions<SecretsConfig>()
                    .Bind(hostingContext.Configuration.GetSection("NsbDbSection"));

                services.AddDbContext<WorkflowDbContext>((serviceProvider, options) => options
                    .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=WorkflowDatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False"));

                using (var sp = services.BuildServiceProvider())
                using (var context = sp.GetRequiredService<WorkflowDbContext>())
                {
                    TasksDbBuilder.UsingDbContext(context)
                        .PopulateTables()
                        .SaveChanges();
                }

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
}