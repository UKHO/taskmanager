using System;
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
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Serilog;
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
            .UseSerilog()
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
            .ConfigureServices((hostingContext, services) =>
            {
                var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;

                var startupLoggingConfig = new StartupLoggingConfig();
                hostingContext.Configuration.GetSection("logging").Bind(startupLoggingConfig);

                var startupSecretsConfig = new StartupSecretsConfig();
                hostingContext.Configuration.GetSection("ContentService").Bind(startupSecretsConfig);
                hostingContext.Configuration.GetSection("subscription").Bind(startupSecretsConfig);
                hostingContext.Configuration.GetSection("LoggingDbSection").Bind(startupSecretsConfig);

                LoggingHelper.SetupLogging(isLocalDebugging, startupLoggingConfig, startupSecretsConfig);

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

                services.AddScoped<IDocumentStatusFactory, DocumentStatusFactory>();

                services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

                services.AddHttpClient<IContentServiceApiClient, ContentServiceApiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        Credentials = new NetworkCredential
                        {
                            UserName = startupSecretsConfig.ContentServiceUsername,
                            Password = startupSecretsConfig.ContentServicePassword,
                            Domain = startupSecretsConfig.ContentServiceDomain
                        }
                    })
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

                var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging,
                    isLocalDebugging ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName);

                services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
                    options.UseSqlServer(workflowDbConnectionString));

                services.AddSingleton<AzureServiceTokenProvider>(new AzureServiceTokenProvider());

                if (isLocalDebugging)
                {
                    using var sp = services.BuildServiceProvider();
                    using var context = sp.GetRequiredService<WorkflowDbContext>();
                    TestWorkflowDatabaseSeeder.UsingDbContext(context).PopulateTables().SaveChanges();
                }

                // Bridge to Terabithia
                using var provider = services.BuildServiceProvider();
                var tokenProvider = provider.GetRequiredService<AzureServiceTokenProvider>();
                hostingContext.Properties.Add("AzureServiceTokenProvider", tokenProvider);

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


                SourceDocumentCoordinatorConfig endpointConfiguration = null;

                if (isLocalDebugging)
                {
                    var localDbServer = nsbConfig.LocalDbServer;

                    nsbSecretsConfig.NsbDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(true, localDbServer, nsbSecretsConfig.NsbInitialCatalog);
                    DatabasesHelpers.ReCreateLocalDb(localDbServer,
                        nsbSecretsConfig.NsbInitialCatalog,
                        DatabasesHelpers.BuildSqlConnectionString(true, localDbServer),
                        ConfigHelpers.IsLocalDevelopment);

                    endpointConfiguration = new SourceDocumentCoordinatorConfig(nsbConfig, nsbSecretsConfig);

                }
                else
                {
                    nsbSecretsConfig.NsbDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(false, nsbSecretsConfig.NsbDataSource, nsbSecretsConfig.NsbInitialCatalog);

                    hostBuilderContext.Properties.TryGetValue("AzureServiceTokenProvider", out var azureServiceTokenProvider);
                    endpointConfiguration = new SourceDocumentCoordinatorConfig(nsbConfig, nsbSecretsConfig, (AzureServiceTokenProvider)azureServiceTokenProvider);
                }



                var serilogTracing = endpointConfiguration.EnableSerilogTracing(Log.Logger);
                serilogTracing.EnableSagaTracing();
                serilogTracing.EnableMessageTracing();

                return endpointConfiguration;
            })
            .UseConsoleLifetime();


            var host = builder.Build();

            var cancellationToken = new WebJobsShutdownWatcher().Token;
            using (host)
            {
                try
                {
                    await host.RunAsync(cancellationToken);
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
        }
    }
}