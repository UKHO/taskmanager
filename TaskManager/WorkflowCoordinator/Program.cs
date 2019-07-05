using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using WorkflowCoordinator.Config;

namespace WorkflowCoordinator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
            .ConfigureHostConfiguration(configHost =>
            {
                // TODO - review this!
                configHost.AddEnvironmentVariables();
            })
            .ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                var keyVaultAddress = hostingContext.Configuration.GetValue<string>("KeyVaultAddress");
                var tokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(tokenProvider.KeyVaultTokenCallback));

                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddAzureKeyVault(keyVaultAddress, keyVaultClient, new DefaultKeyVaultSecretManager());
            })
            .ConfigureLogging((hostingContext, b) =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
            })
            .ConfigureServices((hostingContext, serviceCollection) =>
            {
                serviceCollection.AddOptions<GeneralConfig>()
                    .Bind(hostingContext.Configuration.GetSection("General"));

                serviceCollection.AddOptions<SecretsConfig>()
                    .Bind(hostingContext.Configuration.GetSection("MySecretSection")); // Still the single entry in AKV as example

                serviceCollection.AddScoped<IJobHost, NServiceBusJobHost>();
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