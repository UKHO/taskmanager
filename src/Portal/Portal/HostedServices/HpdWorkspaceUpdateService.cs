using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.HostedServices
{
    public class HpdWorkspaceUpdateService : BackgroundService
    {
        private ILogger<HpdWorkspaceUpdateService> _logger;

        private readonly TimeSpan _updateIntervalSeconds;

        // Hosted service is singleton so we need to safely consume our scoped services
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public HpdWorkspaceUpdateService(ILogger<HpdWorkspaceUpdateService> logger,
                                         IServiceScopeFactory serviceScopeFactory,
                                         IOptions<HpdWorkspaceUpdateServiceConfig> config)
        {

            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            _updateIntervalSeconds = TimeSpan.FromSeconds(config.Value.HpdWorkspaceUpdateIntervalSeconds > 0
                ? config.Value.HpdWorkspaceUpdateIntervalSeconds
                : 3600);

            LogContext.PushProperty("HpdWorkspaceUpdateService", nameof(HpdWorkspaceUpdateService));
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(HpdWorkspaceUpdateService)}: Starting with interval of {_updateIntervalSeconds} seconds.");

            stoppingToken.Register(() =>
                _logger.LogInformation($"{nameof(HpdWorkspaceUpdateService)}: Stopping (received cancellation token)."));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateHpdCacheAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{nameof(HpdWorkspaceUpdateService)}: Failed to update Hpd workspaces.");
                }

                await Task.Delay(_updateIntervalSeconds, stoppingToken);
            }

            _logger.LogInformation($"{nameof(HpdWorkspaceUpdateService)}: Stopping.");
        }


        private async Task UpdateHpdCacheAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var workflowDbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
            var hpdDbContext = scope.ServiceProvider.GetRequiredService<HpdDbContext>();

            try
            {
                var hpdWorkspaces = hpdDbContext.CarisWorkspaces
                    .Select(cw => cw.Name.Trim()) //trim to prevent unique constraint errors
                    .Distinct()
                    .Select(cw => new CachedHpdWorkspace { Name = cw })
                    .OrderBy(cw => cw.Name)
                    .ToList();

                _logger.LogInformation($"{nameof(HpdWorkspaceUpdateService)}: Deleting all workspace rows in WorkflowDatabase table CachedHpdWorkspace.");
                workflowDbContext.Database.ExecuteSqlRaw("DELETE FROM [CachedHpdWorkspace]");

                _logger.LogInformation($"{nameof(HpdWorkspaceUpdateService)}: Adding {hpdWorkspaces.Count()} workspace(s) from HPD, to CachedHpdWorkspace in WorkflowDatabase.");
                workflowDbContext.CachedHpdWorkspace.AddRange(hpdWorkspaces);


                _logger.LogInformation($"{nameof(HpdWorkspaceUpdateService)}: Saving changes.");
                await workflowDbContext.SaveChangesAsync();

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{nameof(HpdWorkspaceUpdateService)} :  Failed to update CachedHpdWorkspace");
                throw e;
            }


        }


    }
}
