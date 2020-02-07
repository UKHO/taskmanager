﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public SessionFileGenerator(WorkflowDbContext dbContext,
            IOptions<SecretsConfig> secretsConfig,
            ILogger<SessionFileGenerator> logger)
        {
            _dbContext = dbContext;
            _secretsConfig = secretsConfig;
            _logger = logger;
        }

        public async Task<SessionFile> PopulateSessionFile(int processId, string userFullName, string taskStage)
        {
            LogContext.PushProperty("PortalResource", nameof(PopulateSessionFile));
            LogContext.PushProperty("UserFullName", userFullName);

            // User details
            HpdUser hpdUser;

            if (userFullName == "Unknown")
            {
                _logger.LogError("ProcessId {ProcessId}: Unable to get username from Active Directory for {UserFullName}");
                throw new ArgumentNullException(nameof(userFullName), "Unable to get username from Active Directory.");
            }

            try
            {
                hpdUser = await _dbContext.HpdUser.SingleAsync(u => u.AdUsername.Equals(userFullName,
                    StringComparison.CurrentCultureIgnoreCase));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserFullName}.");
                throw new InvalidOperationException($"Unable to find HPD username for {userFullName}.",
                    ex.InnerException);
            }

            // Data impact
            var dataImpact = _dbContext.DataImpact.Include(di => di.HpdUsage)
                .FirstOrDefault(di => di.ProcessId == processId);
            var hpdUsageName = dataImpact == null ? string.Empty : dataImpact.HpdUsage.Name;

            // TODO: populate Workspace from relevant table
            string workspaceAffected;

            switch (taskStage)
            {
                case "Assess":
                    var assessData = await _dbContext.DbAssessmentAssessData.FirstAsync(ad => ad.ProcessId == processId);
                    workspaceAffected = assessData.WorkspaceAffected;
                    break;
                case "Verify":
                    var verifyData = await _dbContext.DbAssessmentVerifyData.FirstAsync(ad => ad.ProcessId == processId);
                    workspaceAffected = verifyData.WorkspaceAffected;
                    break;
                default:
                    _logger.LogError($"ProcessId {processId}: Unable to get WorkspaceAffected as task is not at Assess or Verify.");
                    throw new NotImplementedException($"Unable to establish Caris Workspace as task {processId} is at {taskStage}.");
            }

            var sources = await SetSources(processId);

            return new SessionFile
            {
                CarisWorkspace =
                {
                    DataSources = new SessionFile.DataSourcesNode
                    {
                        DataSource = new SessionFile.DataSourceNode
                        {
                            SourceParam = new SessionFile.SourceParamNode
                            {
                                SERVICENAME = _secretsConfig.Value.HpdServiceName,
                                USERNAME = hpdUser.HpdUsername,
                                ASSIGNED_USER = hpdUser.HpdUsername,
                                USAGE = hpdUsageName,
                                WORKSPACE = workspaceAffected,
                                SecureCredentialPlugin = "{guid in here}",
                                SecureCredentialPlugin_UserParam = "UserParameter",
                                HAS_BOUNDARY = "true",
                                OPENED_BY_PROJECT = "true",
                                PROJECT = "19_29_SDRA4.1 registration test2",
                                PROJECT_ID = "53756"
                            },
                            SourceString = "HPD:Project:19_29",
                            UserLayers = ""
                        }
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
                                    Name = "registration test2:Nav 15"
                                }
                            }
                        }
                    },
                    Properties = new SessionFile.PropertiesNode
                    {
                        Property = sources
                    }
                }
            };
        }

        private async Task<List<SessionFile.PropertyNode>> SetSources(int processId)
        {
            var sources = new List<SessionFile.PropertyNode>();

            sources.AddRange(_dbContext.AssessmentData.Where(ad => ad.ProcessId == processId)
                .Select(ad => new SessionFile.PropertyNode()
                {
                    Name = ad.SourceDocumentName,
                    Type = "Source",
                    Source = ""
                }));

            var ddsRows = _dbContext.DatabaseDocumentStatus.Where(ad => ad.ProcessId == processId);

            if (await ddsRows.AnyAsync())
            {
                sources.AddRange(ddsRows.Select(dds => new SessionFile.PropertyNode
                {
                    Name = dds.SourceDocumentName,
                    Type = "Source",
                    Source = ""
                }));
            }

            var attachedLinkedDocs = _dbContext.LinkedDocument.Where(ad =>
                                                            ad.ProcessId == processId
                                                            && !ad.Status.Equals(LinkedDocumentRetrievalStatus.NotAttached.ToString(), StringComparison.OrdinalIgnoreCase));

            if (await attachedLinkedDocs.AnyAsync())
            {
                sources.AddRange(attachedLinkedDocs.Select(ld => new SessionFile.PropertyNode
                {
                    Name = ld.SourceDocumentName,
                    Type = "Source",
                    Source = ""
                }));
            }

            return sources;
        }
    }
}