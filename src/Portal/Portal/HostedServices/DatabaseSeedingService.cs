using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Hosting;

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
            //using var scope = _serviceProvider.CreateScope();
            //var workflowDbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

            TestWorkflowDatabaseSeeder.UsingDbContext(_serviceProvider).PopulateTables().SaveChanges();

            return Task.CompletedTask;
        }

        // Not required
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
