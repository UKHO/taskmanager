using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Portal.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class SessionFileGenerator : ISessionFileGenerator
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOptions<SecretsConfig> _secretsConfig;

        public SessionFileGenerator(WorkflowDbContext dbContext,
            IOptions<SecretsConfig> secretsConfig)
        {
            _dbContext = dbContext;
            _secretsConfig = secretsConfig;
        }

        public async Task<SessionFile> PopulateSessionFile(int processId, string userFullName)
        {
            // TODO: Get data from db to populate session file

            HpdUser hpdUser;

            if (userFullName == "Unknown")
            {
                throw new ArgumentNullException(nameof(userFullName), "Unable to get username from Active Directory.");
            }

            try
            {
                hpdUser = await _dbContext.HpdUser.SingleAsync(u => u.AdUsername.Equals(userFullName,
                    StringComparison.CurrentCultureIgnoreCase));
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Unable to find HPD username for {userFullName}.",
                    ex.InnerException);
            }


            //TODO: Select correct DataImpact if more than one
            var dataImpact = _dbContext.DataImpact.Include(di => di.HpdUsage)
                .FirstOrDefault(di => di.ProcessId == processId);
            var hpdUsageName = dataImpact == null ? string.Empty : dataImpact.HpdUsage.Name;


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
                                SERVICENAME= _secretsConfig.Value.HpdServiceName,
                                USERNAME=hpdUser.HpdUsername,
                                ASSIGNED_USER = hpdUser.HpdUsername,
                                USAGE=hpdUsageName,
                                WORKSPACE="19_29_SDRA4.1 registration test2",
                                SecureCredentialPlugin="{guid in here}",
                                SecureCredentialPlugin_UserParam="UserParameter",
                                HAS_BOUNDARY="true",
                                OPENED_BY_PROJECT="true",
                                PROJECT="19_29_SDRA4.1 registration test2",
                                PROJECT_ID="53756"
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