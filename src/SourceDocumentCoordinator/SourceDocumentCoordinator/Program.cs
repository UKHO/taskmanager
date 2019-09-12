using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using NServiceBus;
using NServiceBus.ObjectBuilder.MSDependencyInjection;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var (keyVaultAddress, keyVaultClient) = SecretsHelpers.SetUpKeyVaultClient();

            var builder = new HostBuilder()
            .UseEnvironment(Environment.GetEnvironmentVariable("ENVIRONMENT"))
            .ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager()).Build();

                var azureAppConfConnectionString = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                config.AddAzureAppConfiguration(azureAppConfConnectionString);
            })
            .ConfigureLogging((hostingContext, b) =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices((hostingContext, services) =>
            {
                var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;

                var startupConfig = new StartupConfig();
                hostingContext.Configuration.GetSection("nsb").Bind(startupConfig);
                hostingContext.Configuration.GetSection("urls").Bind(startupConfig);
                hostingContext.Configuration.GetSection("databases").Bind(startupConfig);

                var endpointConfiguration = new EndpointConfiguration(startupConfig.SourceDocumentCoordinatorName);
                services.AddSingleton<EndpointConfiguration>(endpointConfiguration);

                services.AddOptions<UriConfig>()
                    .Bind(hostingContext.Configuration.GetSection("urls"));
                services.AddOptions<GeneralConfig>()
                    .Bind(hostingContext.Configuration.GetSection("apis"));
                services.AddOptions<GeneralConfig>()
                    .Bind(hostingContext.Configuration.GetSection("nsb"));
                services.AddOptions<GeneralConfig>()
                    .Bind(hostingContext.Configuration.GetSection("databases"));
                services.AddOptions<SecretsConfig>()
                    .Bind(hostingContext.Configuration.GetSection("NsbDbSection"));

                services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

                var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging,
                    isLocalDebugging ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName);

                var connection = new SqlConnection(workflowDbConnectionString)
                {
                    AccessToken = isLocalDebugging ?
                        null :
                        new AzureServiceTokenProvider().GetAccessTokenAsync(startupConfig.AzureDbTokenUrl.ToString()).Result
                };
                services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
                    options.UseSqlServer(connection));

                if (isLocalDebugging)
                {
                    using (var sp = services.BuildServiceProvider())
                    using (var context = sp.GetRequiredService<WorkflowDbContext>())
                    {
                        TestWorkflowDatabaseSeeder.UsingDbContext(context).PopulateTables().SaveChanges();
                    }
                }

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