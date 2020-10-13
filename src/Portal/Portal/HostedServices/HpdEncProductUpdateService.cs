using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Portal.Configuration;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.HostedServices
{
    public class HpdEncProductUpdateService : BackgroundService
    {
        private readonly ILogger<HpdEncProductUpdateService> _logger;
        private readonly TimeSpan _updateIntervalSeconds;

        // Hosted service is singleton so we need to safely consume our scoped services
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public HpdEncProductUpdateService(ILogger<HpdEncProductUpdateService> logger,
            IOptions<HpdEncProductUpdateServiceConfig> config,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            _updateIntervalSeconds = TimeSpan.FromSeconds(config.Value.HpdEncProductUpdateIntervalSeconds > 0
                ? config.Value.HpdEncProductUpdateIntervalSeconds
                : 3600);

            LogContext.PushProperty("HpdEncProductUpdateService", nameof(HpdEncProductUpdateService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(HpdEncProductUpdateService)}: Starting with interval of {_updateIntervalSeconds} seconds.");

            stoppingToken.Register(() =>
                _logger.LogInformation($"{nameof(HpdEncProductUpdateService)}: Stopping (received cancellation token)."));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateHpdEncProductAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{nameof(HpdEncProductUpdateService)}: Failed to update enc products.");
                }

                await Task.Delay(_updateIntervalSeconds, stoppingToken);
            }

            _logger.LogInformation($"{nameof(HpdEncProductUpdateService)}: Stopping.");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", 
            "SecurityIntelliSenseCS:MS Security rules violation", 
            Justification = "HPD SQL Command with CommandType.Text is necessary as it is our only option to retrieve data as Oracle.EF will not work here.")]
        private async Task UpdateHpdEncProductAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var hpdDbContext = scope.ServiceProvider.GetRequiredService<HpdDbContext>();
            var workflowDbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
            var hpdEncProducts = new List<CachedHpdEncProduct>();

            var commandText = "select distinct name " +
                                    "from HPDOWNER.VECTOR_PRODUCT_VIEW " +
                                    "where product_status = 'Active' and type_key = 'ENC' " +
                                    "order by name";

            try
            {
                _logger.LogInformation($"{nameof(HpdEncProductUpdateService)}: Retrieving all enc product rows from HPD.");

                var connection = hpdDbContext.Database.GetDbConnection();
                await using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = commandText;

                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    hpdEncProducts.Add(new CachedHpdEncProduct { Name = reader[0].ToString() });
                }
            }
            catch (OracleException e)
            {
                _logger.LogError(e, $"{nameof(HpdEncProductUpdateService)}: Error retrieving all enc product rows from HPD.");
                throw e;
            }

            try
            {
                _logger.LogInformation($"{nameof(HpdEncProductUpdateService)}: Deleting all enc product rows in WorkflowDatabase table CachedHpdEncProduct.");
                workflowDbContext.CachedHpdEncProduct.RemoveRange(workflowDbContext.CachedHpdEncProduct);

                _logger.LogInformation($"{nameof(HpdEncProductUpdateService)}: Adding {hpdEncProducts.Count} workspace from HPD, to WorkflowDatabase.");
                workflowDbContext.CachedHpdEncProduct.AddRange(hpdEncProducts);

                _logger.LogInformation($"{nameof(HpdEncProductUpdateService)}: Saving changes.");
                await workflowDbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{nameof(HpdEncProductUpdateService)}: Error updating enc product rows in WorkflowDatabase table CachedHpdEncProduct.");
                throw e;
            }
        }
    }
}