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
using Portal.Configuration;
using Portal.Models;
using Serilog.Context;
using WorkflowDatabase.EF;

namespace Portal.Pages.DbAssessment
{
    public class _EditDatabaseModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<_EditDatabaseModel> _logger;
        private readonly IOptions<GeneralConfig> _generalConfig;

        [DisplayName("Select CARIS Workspace:")]
        public string SelectedCarisWorkspace { get; set; }

        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        public _EditDatabaseModel(WorkflowDbContext dbContext, ILogger<_EditDatabaseModel> logger, IOptions<GeneralConfig> generalConfig)
        {
            _dbContext = dbContext;
            _logger = logger;
            _generalConfig = generalConfig;
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

            var sessionFile = PopulateSessionFile(processId);

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

        private SessionFile PopulateSessionFile(int processId)
        {
            // TODO: Get data from db to populate session file

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
                                SERVICENAME="servicenamehere",
                                USERNAME="testuser",
                                ASSIGNED_USER="testuser",
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
                        Property = new List<SessionFile.PropertyNode>
                        {
                            new SessionFile.PropertyNode
                            {
                                Name = "HDB Source Registry",
                                Property = new SessionFile.PropertyNode
                                {
                                    Source = "filesharepathexample1",
                                    Name = "RSDRA2019000000029_2",
                                    Type = "source",
                                    Property = new SessionFile.PropertyNode
                                    {
                                        Name = "RSDRA2019000000029_2",
                                        Type = "layer",
                                        Item = new SessionFile.ItemNode
                                        {
                                            Name = "Image Transparency %",
                                            Group = "Display",
                                            Value = "50"
                                        },
                                    }
                                },
                                Type = "Group"
                            },
                            new SessionFile.PropertyNode
                            {
                                Source = ":HPD:Project:|19_29_SDRA4.1 registration test2",
                                Name = "HPD:Project:|19_2",
                                Property = new SessionFile.PropertyNode
                                {
                                    Name = "Nav 15 Large[6000-69999",
                                    Type = "layer",
                                    Item = new SessionFile.ItemNode
                                    {
                                        Name = "Override Colour",
                                        Group = "General",
                                        Value = "0"
                                    }
                                },
                                Type = "Source"
                            }
                        }
                    }
                }
            };
        }
    }
}