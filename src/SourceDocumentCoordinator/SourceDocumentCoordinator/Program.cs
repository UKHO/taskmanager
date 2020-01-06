using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Helpers;
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
                var azureAppConfConnectionString = Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                config.AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager())
                      .AddAzureAppConfiguration(azureAppConfConnectionString)
                      .Build();
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

                services.AddOptions<UriConfig>()
                    .Bind(hostingContext.Configuration.GetSection("urls"));

                services.AddOptions<GeneralConfig>()
                    .Bind(hostingContext.Configuration.GetSection("apis"))
                    .Bind(hostingContext.Configuration.GetSection("nsb"))
                    .Bind(hostingContext.Configuration.GetSection("databases"))
                    .Bind(hostingContext.Configuration.GetSection("path"));

                services.AddOptions<SecretsConfig>()
                    .Bind(hostingContext.Configuration.GetSection("NsbDbSection"));

                var startupSecretConfig = new StartupSecretsConfig();
                hostingContext.Configuration.GetSection("ContentService").Bind(startupSecretConfig);

                services.AddScoped<IDocumentStatusFactory, DocumentStatusFactory>();

                services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

                services.AddHttpClient<IContentServiceApiClient, ContentServiceApiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        Credentials = new NetworkCredential
                        {
                            UserName = startupSecretConfig.ContentServiceUsername,
                            Password = startupSecretConfig.ContentServicePassword,
                            Domain = startupSecretConfig.ContentServiceDomain
                        }
                    })
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
                    using var sp = services.BuildServiceProvider();
                    using var context = sp.GetRequiredService<WorkflowDbContext>();
                    TestWorkflowDatabaseSeeder.UsingDbContext(context).PopulateTables().SaveChanges();
                }
            })
            .UseNServiceBus(hostBuilderContext =>
            {
                var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;

                var nsbConfig = new NsbConfig();
                hostBuilderContext.Configuration.GetSection("nsb").Bind(nsbConfig);
                hostBuilderContext.Configuration.GetSection("urls").Bind(nsbConfig);
                hostBuilderContext.Configuration.GetSection("databases").Bind(nsbConfig);
                nsbConfig.IsLocalDevelopment = isLocalDebugging;

                var nsbSecretsConfig = new NsbSecretsConfig();
                hostBuilderContext.Configuration.GetSection("NsbDbSection").Bind(nsbSecretsConfig);

                var endpointConfiguration = new SourceDocumentCoordinatorConfig(nsbConfig, nsbSecretsConfig);

                if (isLocalDebugging)
                {
                    var localDbServer = nsbConfig.LocalDbServer;

                    nsbSecretsConfig.NsbDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(true, localDbServer, nsbSecretsConfig.NsbInitialCatalog);
                    DatabasesHelpers.ReCreateLocalDb(localDbServer,
                        nsbSecretsConfig.NsbInitialCatalog,
                        DatabasesHelpers.BuildSqlConnectionString(true, localDbServer),
                        ConfigHelpers.IsLocalDevelopment);
                }
                else
                {
                    nsbSecretsConfig.NsbDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(false, nsbSecretsConfig.NsbDataSource, nsbSecretsConfig.NsbInitialCatalog);

                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var azureDbTokenUrl = nsbConfig.AzureDbTokenUrl;
                    nsbSecretsConfig.AzureAccessToken = azureServiceTokenProvider.GetAccessTokenAsync(azureDbTokenUrl.ToString()).Result;
                }

                return endpointConfiguration;
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