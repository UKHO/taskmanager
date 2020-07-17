using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Helpers.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.HostedServices
{
    public class AdUserUpdateService : BackgroundService
    {
        private readonly ILogger<AdUserUpdateService> _logger;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly AdUserUpdateServiceSecrets _secrets;
        private readonly TimeSpan _updateIntervalSeconds;

        // Hosted service is singleton so we need to safely consume our scoped services
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AdUserUpdateService(ILogger<AdUserUpdateService> logger,
            IOptions<AdUserUpdateServiceConfig> config,
            IOptions<AdUserUpdateServiceSecrets> secrets,
            IAdDirectoryService adDirectoryService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _adDirectoryService = adDirectoryService;
            _secrets = secrets.Value;
            _serviceScopeFactory = serviceScopeFactory;

            _updateIntervalSeconds = TimeSpan.FromSeconds(config.Value.AdUpdateIntervalSeconds > 0
                ? config.Value.AdUpdateIntervalSeconds
                : 3600);

            LogContext.PushProperty("AdUserUpdateService", nameof(AdUserUpdateService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(AdUserUpdateService)}: Starting with interval of {_updateIntervalSeconds} seconds.");

            stoppingToken.Register(() =>
                _logger.LogInformation($"{nameof(AdUserUpdateService)}: Stopping (received cancellation token)."));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateDbFromAdAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{nameof(AdUserUpdateService)}: Failed to update users from AD.");
                }

                await Task.Delay(_updateIntervalSeconds, stoppingToken);
            }

            _logger.LogInformation($"{nameof(AdUserUpdateService)}: Stopping.");
        }

        private async Task UpdateDbFromAdAsync()
        {
            var adGroupMembers =
                await _adDirectoryService.GetGroupMembersFromAdAsync(ExtractGuidsFromStr(_secrets.AdUserGroups));

            using var scope = _serviceScopeFactory.CreateScope();
            var workflowDbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

            var currentAdUsers = await workflowDbContext.AdUsers.ToListAsync();

            var newAdUsers = adGroupMembers.Where(m =>
                currentAdUsers.All(c => c.UserPrincipalName != m.UserPrincipalName)).Select(n => new AdUser
                {
                    DisplayName = n.DisplayName,
                    UserPrincipalName = n.UserPrincipalName,
                    // TODO hook into checks
                    LastCheckedDate = DateTime.UtcNow
                }).ToList(); // avoid multiple iterations for counting with ToList()

            if (newAdUsers.Count > 0) _logger.LogInformation($"{nameof(AdUserUpdateService)}: {newAdUsers.Count} new users added to database from AD.");

            workflowDbContext.AdUsers.AddRange(newAdUsers);

            await workflowDbContext.SaveChangesAsync();
        }

        private IEnumerable<Guid> ExtractGuidsFromStr(string adUserGroups)
        {
            var guidCollection = adUserGroups.Split(',')
                    .Where(x => Guid.TryParse(x, out _))
                    .Select(Guid.Parse).ToList();

            var diff = guidCollection.Count - _secrets.AdUserGroups.Length;
            if (diff > 0) _logger.LogWarning($"{nameof(AdUserUpdateService)}: {diff} invalid GUIDs supplied in configuration.");

            return guidCollection;
        }
    }
}