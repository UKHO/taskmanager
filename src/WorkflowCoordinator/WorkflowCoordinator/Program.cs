using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
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
               var isLocalDebugging = hostingContext.HostingEnvironment.IsDevelopment() && Debugger.IsAttached;

               var startupConfig = new StartupConfig();
               hostingContext.Configuration.GetSection("nsb").Bind(startupConfig);
               hostingContext.Configuration.GetSection("urls").Bind(startupConfig);
               hostingContext.Configuration.GetSection("databases").Bind(startupConfig);

               var endpointConfiguration = new EndpointConfiguration(startupConfig.WorkflowCoordinatorName);
               services.AddSingleton<EndpointConfiguration>(endpointConfiguration);

                services.AddOptions<GeneralConfig>()
                    .Bind(hostingContext.Configuration.GetSection("nsb"))
                    .Bind(hostingContext.Configuration.GetSection("apis"))
                    .Bind(hostingContext.Configuration.GetSection("urls"))
                    .Bind(hostingContext.Configuration.GetSection("databases"))
                    .Bind(hostingContext.Configuration.GetSection("k2"));

               services.AddOptions<SecretsConfig>()
                   .Bind(hostingContext.Configuration.GetSection("NsbDbSection"))
                   .Bind(hostingContext.Configuration.GetSection("K2RestApi"));

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

               services.AddHttpClient<IDataServiceApiClient, DataServiceApiClient>()
                   .SetHandlerLifetime(TimeSpan.FromMinutes(5));

               var secretConfig = new SecretsConfig();
               hostingContext.Configuration.GetSection("NsbDbSection").Bind(secretConfig);
               hostingContext.Configuration.GetSection("K2RestApi").Bind(secretConfig);

               services.AddHttpClient<IWorkflowServiceApiClient, WorkflowServiceApiClient>()
                   .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                   .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                   {
                       ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                       Credentials = new NetworkCredential(secretConfig.NsbToK2ApiUsername, secretConfig.NsbToK2ApiPassword)
                    });

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