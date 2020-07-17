using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Portal.Configuration;

namespace Portal.HostedServices
{
    public class DatabaseSeedingService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabaseSeedingService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var isLocalDevelopment = ConfigHelpers.IsLocalDevelopment;

            using var scope = _serviceProvider.CreateScope();
            var startupConfig = scope.ServiceProvider.GetRequiredService<IOptions<StartupConfig>>().Value;

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDevelopment,
                isLocalDevelopment ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName);
            TestWorkflowDatabaseSeeder.UsingDbConnectionString(workflowDbConnectionString).PopulateTables().SaveChanges();

            return Task.CompletedTask;
        }

        // Not required
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
