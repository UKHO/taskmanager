using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Common.Helpers;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Serilog;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.MappingProfiles;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var (keyVaultAddress, keyVaultClient) = SecretsHelpers.SetUpKeyVaultClient();
            var isLocalDebugging = false;

            var builder = new HostBuilder()
                .UseEnvironment(ConfigHelpers.HostBuilderEnvironment)
                .UseSerilog()
                .ConfigureWebJobs((context, b) => { b.AddAzureStorageCoreServices(); })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var azureAppConfConnectionString =
                        Environment.GetEnvironmentVariable("AZURE_APP_CONFIGURATION_CONNECTION_STRING");

                    config.AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager())
                        .AddAzureAppConfiguration(azureAppConfConnectionString)
                        .Build();
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    var startupLoggingConfig = new StartupLoggingConfig();
                    hostingContext.Configuration.GetSection("logging").Bind(startupLoggingConfig);

                    var startupSecretsConfig = new StartupSecretsConfig();
                    hostingContext.Configuration.GetSection("K2RestApi").Bind(startupSecretsConfig);
                    hostingContext.Configuration.GetSection("LoggingDbSection").Bind(startupSecretsConfig);
                    hostingContext.Configuration.GetSection("PCPEventService").Bind(startupSecretsConfig);

                    LoggingHelper.SetupLogging(isLocalDebugging, startupLoggingConfig, startupSecretsConfig);

                    var startupConfig = new StartupConfig();
                    hostingContext.Configuration.GetSection("nsb").Bind(startupConfig);
                    hostingContext.Configuration.GetSection("urls").Bind(startupConfig);
                    hostingContext.Configuration.GetSection("databases").Bind(startupConfig);

                    services.AddOptions<StartupConfig>()
                        .Bind(hostingContext.Configuration.GetSection("nsb"));

                    services.AddOptions<GeneralConfig>()
                        .Bind(hostingContext.Configuration.GetSection("nsb"))
                        .Bind(hostingContext.Configuration.GetSection("apis"))
                        .Bind(hostingContext.Configuration.GetSection("databases"))
                        .Bind(hostingContext.Configuration.GetSection("k2"));

                    services.AddOptions<UriConfig>()
                        .Bind(hostingContext.Configuration.GetSection("urls"));

                    var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging,
                        isLocalDebugging ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer,
                        startupConfig.WorkflowDbName);

                    services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
                        options.UseLazyLoadingProxies().UseSqlServer(workflowDbConnectionString));

                    if (isLocalDebugging)
                    {
                        using var sp = services.BuildServiceProvider();
                        TestWorkflowDatabaseSeeder.UsingDbConnectionString(workflowDbConnectionString).PopulateTables().SaveChanges();
                    }

                    services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                        .SetHandlerLifetime(TimeSpan.FromMinutes(5));

                    services.AddHttpClient<IPcpEventServiceApiClient, PcpEventServiceApiClient>()
                        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = (message, certificate, arg3, arg4) => true,
                            Credentials = new NetworkCredential(startupSecretsConfig.PCPEventServiceUsername, startupSecretsConfig.PCPEventServicePassword)
                        });

                    services.AddOptions<SecretsConfig>()
                        .Bind(hostingContext.Configuration.GetSection("NsbDbSection"));

                    // Auto mapper config
                    var mappingConfig = new MapperConfiguration(mc =>
                    {
                        mc.AddProfile(new AssessmentDataMappingProfile());
                    });
                    var mapper = mappingConfig.CreateMapper();
                    services.AddSingleton(mapper);

                    services.AddHttpClient<IWorkflowServiceApiClient, WorkflowServiceApiClient>()
                        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                            Credentials = new NetworkCredential(startupSecretsConfig.K2RestApiUsername,
                                startupSecretsConfig.K2RestApiPassword)
                        });
                })
                .UseNServiceBus(hostBuilderContext =>
                {
                    var nsbConfig = new NsbConfig();
                    hostBuilderContext.Configuration.GetSection("nsb").Bind(nsbConfig);
                    hostBuilderContext.Configuration.GetSection("urls").Bind(nsbConfig);
                    hostBuilderContext.Configuration.GetSection("databases").Bind(nsbConfig);
                    nsbConfig.IsLocalDevelopment = isLocalDebugging;

                    var nsbSecretsConfig = new NsbSecretsConfig();
                    hostBuilderContext.Configuration.GetSection("NsbDbSection").Bind(nsbSecretsConfig);

                    WorkflowCoordinatorConfig endpointConfiguration = null;

                    if (isLocalDebugging)
                    {
                        var localDbServer = nsbConfig.LocalDbServer;

                        nsbSecretsConfig.NsbDbConnectionString =
                            DatabasesHelpers.BuildSqlConnectionString(true, localDbServer,
                                nsbSecretsConfig.NsbInitialCatalog);
                        DatabasesHelpers.ReCreateLocalDb(localDbServer,
                            nsbSecretsConfig.NsbInitialCatalog,
                            DatabasesHelpers.BuildSqlConnectionString(true, localDbServer),
                            ConfigHelpers.IsLocalDevelopment);

                        endpointConfiguration = new WorkflowCoordinatorConfig(nsbConfig, nsbSecretsConfig);
                    }
                    else
                    {
                        nsbSecretsConfig.NsbDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(false,
                            nsbSecretsConfig.NsbDataSource, nsbSecretsConfig.NsbInitialCatalog);

                        endpointConfiguration = new WorkflowCoordinatorConfig(nsbConfig, nsbSecretsConfig);

                        endpointConfiguration.SendHeartbeatTo(
                            nsbConfig.ServiceControlQueue,
                            TimeSpan.FromSeconds(15),
                            TimeSpan.FromSeconds(30));

                        endpointConfiguration.UniquelyIdentifyRunningInstance()
                            .UsingCustomIdentifier(nsbConfig.WorkflowCoordinatorUniqueIdentifier);
                    }


                    var serilogTracing = endpointConfiguration.EnableSerilogTracing(Log.Logger);
                    serilogTracing.EnableSagaTracing();
                    serilogTracing.EnableMessageTracing();

                    return endpointConfiguration;
                })
                .UseConsoleLifetime();

            IHost host = null;

            try
            {
                host = builder.Build();

                await host.WorkflowCoordinatorRunAsync();

                var cancellationToken = new WebJobsShutdownWatcher().Token;
                await host.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
                host?.Dispose();
            }
        }
    }
}