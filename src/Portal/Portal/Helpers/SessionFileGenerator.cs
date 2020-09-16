using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Auth;
using Portal.Configuration;
using Portal.Models;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class SessionFileGenerator : ISessionFileGenerator
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOptions<SecretsConfig> _secretsConfig;
        private readonly ILogger<SessionFileGenerator> _logger;
        private readonly IPortalUserDbService _portalUserDbService;

        public SessionFileGenerator(WorkflowDbContext dbContext,
            IOptions<SecretsConfig> secretsConfig,
            ILogger<SessionFileGenerator> logger,
            IPortalUserDbService portalUserDbService)
        {
            _dbContext = dbContext;
            _secretsConfig = secretsConfig;
            _logger = logger;
            _portalUserDbService = portalUserDbService;
        }

        public async Task<SessionFile> PopulateSessionFile(int processId,
            string userPrincipalName,
            string workspaceAffected,
            CarisProjectDetails carisProjectDetails,
            List<string> selectedHpdUsages,
            List<string> selectedSources)
        {
            LogContext.PushProperty("PortalResource", nameof(PopulateSessionFile));
            LogContext.PushProperty("UserPrincipalName", userPrincipalName);

            // User details
            HpdUser hpdUser;

            if (!await _portalUserDbService.ValidateUserAsync(userPrincipalName))
            {
                _logger.LogError("ProcessId {ProcessId}: Unable to get username from Active Directory for {UserPrincipalName}");
                throw new ArgumentNullException(nameof(userPrincipalName), "Unable to get username from Active Directory.");
            }

            try
            {
                hpdUser = await _dbContext.HpdUser.SingleAsync(u => u.AdUser.UserPrincipalName==userPrincipalName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserPrincipalName}.");
                throw new InvalidOperationException($"Unable to find HPD username for {userPrincipalName}.",
                    ex.InnerException);
            }

            if (!await _dbContext.CachedHpdWorkspace.AnyAsync(c =>
                c.Name == workspaceAffected))
            {
                _logger.LogError($"Current Caris Workspace {workspaceAffected} is invalid.");
                throw new InvalidOperationException($"Current Caris Workspace {workspaceAffected} is invalid.");
            }

            var sources = new List<SessionFile.DataSourceNode>();

            SetUsages(sources, hpdUser, workspaceAffected, carisProjectDetails, selectedHpdUsages);

            if (selectedSources?.Count > 0)
                SetSources(sources, selectedSources);

            return new SessionFile
            {
                Version = "1.1",
                DataSources = new SessionFile.DataSourcesNode
                {
                    DataSource = sources
                },
                Views = new SessionFile.ViewsNode
                {
                    View = new SessionFile.ViewNode
                    {
                        DisplayState = new SessionFile.DisplayStateNode
                        {
                            DisplayLayer = new SessionFile.DisplayLayerNode
                            {
                                Visible = "true",
                                Expanded = "false",
                                Name = $":HPD:Project:|{carisProjectDetails.ProjectName}:{selectedHpdUsages.First()}"
                            }
                        }
                    }
                }
            };
        }

        private void SetUsages(List<SessionFile.DataSourceNode> sources, HpdUser hpdUser, string workspaceAffected, CarisProjectDetails carisProjectDetails, List<string> selectedHpdUsages)
        {
            sources.Add(new SessionFile.DataSourceNode()
            {
                SourceParam = new SessionFile.SourceParamNode
                {
                    SERVICENAME = _secretsConfig.Value.HpdServiceName,
                    USERNAME = hpdUser.HpdUsername,
                    ASSIGNED_USER = hpdUser.HpdUsername,
                    USAGE = selectedHpdUsages.First(),
                    WORKSPACE = workspaceAffected,
                    OPENED_BY_PROJECT = "true",
                    PROJECT = carisProjectDetails.ProjectName,
                    PROJECT_ID = carisProjectDetails.ProjectId.ToString(),
                    SELECTEDPROJECTUSAGES = new SessionFile.SelectedProjectUsages()
                    {
                        Value = selectedHpdUsages
                    }
                },
                SourceString = $":HPD:Project:|{carisProjectDetails.ProjectName}",
                UserLayers = ""

            });
        }

        private void SetSources(List<SessionFile.DataSourceNode> sources, List<string> selectedSources)
        {

            sources.AddRange(selectedSources.Select(s => new SessionFile.DataSourceNode()
            {
                SourceString = s,
                SourceParam = new SessionFile.SourceParamNode()
                {
                    DisplayName = new SessionFile.DisplayNameNode()
                    {
                        Value = Path.GetFileNameWithoutExtension(s)
                    },
                    SurfaceString = new SessionFile.SurfaceStringNode()
                    {
                        Value = s
                    }
                }
            }));
        }
    }
}