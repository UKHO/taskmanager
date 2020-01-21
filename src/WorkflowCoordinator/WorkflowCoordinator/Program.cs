﻿using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Common.Helpers;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
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

            var builder = new HostBuilder()
                .UseEnvironment(Environment.GetEnvironmentVariable("ENVIRONMENT"))
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
                    var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;

                    var startupLoggingConfig = new StartupLoggingConfig();
                    hostingContext.Configuration.GetSection("logging").Bind(startupLoggingConfig);

                    var startupSecretsConfig = new StartupSecretsConfig();
                    hostingContext.Configuration.GetSection("K2RestApi").Bind(startupSecretsConfig);
                    hostingContext.Configuration.GetSection("LoggingDbSection").Bind(startupSecretsConfig);

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

                    var connection = new SqlConnection(workflowDbConnectionString)
                    {
                        AccessToken = isLocalDebugging
                            ? null
                            : new AzureServiceTokenProvider()
                                .GetAccessTokenAsync(startupConfig.AzureDbTokenUrl.ToString()).Result
                    };
                    services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
                        options.UseSqlServer(connection));

                    if (isLocalDebugging)
                    {
                        using var sp = services.BuildServiceProvider();
                        using var context = sp.GetRequiredService<WorkflowDbContext>();
                        TestWorkflowDatabaseSeeder.UsingDbContext(context).PopulateTables().SaveChanges();
                    }

                    services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                        .SetHandlerLifetime(TimeSpan.FromMinutes(5));

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
                    var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;

                    var nsbConfig = new NsbConfig();
                    hostBuilderContext.Configuration.GetSection("nsb").Bind(nsbConfig);
                    hostBuilderContext.Configuration.GetSection("urls").Bind(nsbConfig);
                    hostBuilderContext.Configuration.GetSection("databases").Bind(nsbConfig);
                    nsbConfig.IsLocalDevelopment = isLocalDebugging;

                    var nsbSecretsConfig = new NsbSecretsConfig();
                    hostBuilderContext.Configuration.GetSection("NsbDbSection").Bind(nsbSecretsConfig);

                    var endpointConfiguration = new WorkflowCoordinatorConfig(nsbConfig, nsbSecretsConfig);

                    var serilogTracing = endpointConfiguration.EnableSerilogTracing(Log.Logger);
                    serilogTracing.EnableSagaTracing();
                    serilogTracing.EnableMessageTracing();

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
                    }
                    else
                    {
                        nsbSecretsConfig.NsbDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(false,
                            nsbSecretsConfig.NsbDataSource, nsbSecretsConfig.NsbInitialCatalog);

                        var azureServiceTokenProvider = new AzureServiceTokenProvider();
                        var azureDbTokenUrl = nsbConfig.AzureDbTokenUrl;
                        nsbSecretsConfig.AzureAccessToken = azureServiceTokenProvider
                            .GetAccessTokenAsync(azureDbTokenUrl.ToString()).Result;
                    }

                    return endpointConfiguration;
                })
                .UseConsoleLifetime();

            var host = builder.Build();

            using (host)
            {
                try
                {
                    await host.WorkflowCoordinatorRunAsync();
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