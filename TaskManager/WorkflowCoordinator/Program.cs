﻿using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkflowCoordinator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .UseEnvironment("Development")
                .ConfigureWebJobs(b =>
                {
                    b.UseHostId("TBC")
                        .AddAzureStorageCoreServices();
                })
                .ConfigureAppConfiguration(b =>
                {
                    // configure me
                })
                .ConfigureLogging((context, b) =>
                {
                    b.SetMinimumLevel(LogLevel.Debug);

                })
                .ConfigureServices(serviceCollection =>
                {
                    // TODO ... what scope?
                    serviceCollection.AddScoped<IJobHost, TestHost>();
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
