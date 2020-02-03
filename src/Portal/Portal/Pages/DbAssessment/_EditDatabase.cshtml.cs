using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Auth;
using Portal.Configuration;
using Portal.Models;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _EditDatabaseModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<_EditDatabaseModel> _logger;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private IUserIdentityService _userIdentityService;
        private readonly IOptions<SecretsConfig> _secretsConfig;

        [DisplayName("Select CARIS Workspace:")]
        public string SelectedCarisWorkspace { get; set; }

        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public _EditDatabaseModel(WorkflowDbContext dbContext, ILogger<_EditDatabaseModel> logger,
            IOptions<GeneralConfig> generalConfig,
            IUserIdentityService userIdentityService,
            IOptions<SecretsConfig> secretsConfig)
        {
            _dbContext = dbContext;
            _logger = logger;
            _generalConfig = generalConfig;
            _userIdentityService = userIdentityService;
            _secretsConfig = secretsConfig;
        }

        public async Task OnGetAsync(int processId)
        {
        }

        public async Task<JsonResult> OnGetWorkspacesAsync()
        {
            var cachedHpdWorkspaces = await _dbContext.CachedHpdWorkspace.Select(c => c.Name).ToListAsync();
            return new JsonResult(cachedHpdWorkspaces);
        }

        public async Task<IActionResult> OnGetLaunchSourceEditorAsync(int processId, string taskStage)
        {
            LogContext.PushProperty("ActivityName", taskStage);
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnGetLaunchSourceEditorAsync));

            _logger.LogInformation("Launching Source Editor with: ProcessId: {ProcessId}; ActivityName: {ActivityName};");

            var sessionFile = await PopulateSessionFile(processId);

            var serializer = new XmlSerializer(typeof(SessionFile));

            var fs = new MemoryStream();
            try
            {
                serializer.Serialize(fs, sessionFile);

                fs.Position = 0;

                return File(fs, MediaTypeNames.Application.Xml, _generalConfig.Value.SessionFilename);
            }
            catch (InvalidOperationException ex)
            {

                fs.Dispose();
                _logger.LogError(ex, "Failed to serialize Caris session file.");
                return StatusCode(500);
            }
            catch (Exception ex)
            {
                fs.Dispose();
                _logger.LogError(ex, "Failed to generate session file.");
                return StatusCode(500);
            }
        }

        private async Task<SessionFile> PopulateSessionFile(int processId)
        {
            // TODO: Get data from db to populate session file

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);
            HpdUser hpdUser;

            if (UserFullName == "Unknown")
            {
                throw new ArgumentNullException(nameof(UserFullName), "Unable to get username from Active Directory. Please ensure user exists in AD group.");
            }

            try
            {
                hpdUser = await _dbContext.HpdUser.SingleAsync(u => u.AdUsername.Equals(UserFullName,
                     StringComparison.CurrentCultureIgnoreCase));
            }
            catch (InvalidOperationException ex)
            {
                hpdUser = new HpdUser();
                //throw new InvalidOperationException($"Unable to find HPD username for {UserFullName}. Please ensure the relevant row has been created in that table there.",
                //    ex.InnerException);
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
                                SERVICENAME=_secretsConfig.Value.HpdServiceName,
                                USERNAME=hpdUser.HpdUsername,
                                ASSIGNED_USER = hpdUser.HpdUsername,
                                USAGE="Nav 15 Large[6000-69999]",
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

            var linkedDocs = _dbContext.LinkedDocument.Where(ad => ad.ProcessId == processId);

            if (await linkedDocs.AnyAsync())
            {
                sources.AddRange(linkedDocs.Select(ld => new SessionFile.PropertyNode
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