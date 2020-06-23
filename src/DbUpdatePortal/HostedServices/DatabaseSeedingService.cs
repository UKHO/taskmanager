using Common.Helpers;
using DbUpdateWorkflowDatabase.EF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DbUpdatePortal.HostedServices
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
            using var scope = _serviceProvider.CreateScope();
            var workflowDbContext = scope.ServiceProvider.GetRequiredService<DbUpdateWorkflowDbContext>();

            DbUpdateWorkflowDatabaseSeeder.UsingDbContext(workflowDbContext).PopulateTables().SaveChanges();

            return Task.CompletedTask;
        }

        // Not required
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
