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
using Portal.Helpers;
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

        [DisplayName("Select CARIS Workspace:")]
        public string SelectedCarisWorkspace { get; set; }

        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        private string _userFullName;
        private readonly ISessionFileGenerator _sessionFileGenerator;

        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public WorkflowDbContext DbContext
        {
            get { return _dbContext; }
        }

        public _EditDatabaseModel(WorkflowDbContext dbContext, ILogger<_EditDatabaseModel> logger,
            IOptions<GeneralConfig> generalConfig,
            IUserIdentityService userIdentityService,
            ISessionFileGenerator sessionFileGenerator)
        {
            _dbContext = dbContext;
            _logger = logger;
            _generalConfig = generalConfig;
            _userIdentityService = userIdentityService;
            _sessionFileGenerator = sessionFileGenerator;
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

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);
            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(processId, UserFullName);

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
    }
}