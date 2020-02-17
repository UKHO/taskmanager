using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Common.Helpers;
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
    [TypeFilter(typeof(JavascriptError))]
    public class _EditDatabaseModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<_EditDatabaseModel> _logger;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private IUserIdentityService _userIdentityService;
        private readonly ISessionFileGenerator _sessionFileGenerator;
        private readonly ICarisProjectHelper _carisProjectHelper;

        [BindProperty(SupportsGet = true)]
        [DisplayName("Select CARIS Workspace:")]
        public string SelectedCarisWorkspace { get; set; }

        [BindProperty(SupportsGet = true)]
        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        public string SessionFilename { get; set; }

        private string _userFullName;

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
            ISessionFileGenerator sessionFileGenerator,
            ICarisProjectHelper carisProjectHelper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _generalConfig = generalConfig;
            _userIdentityService = userIdentityService;
            _sessionFileGenerator = sessionFileGenerator;
            _carisProjectHelper = carisProjectHelper;
        }

        public async Task OnGetAsync(int processId, string taskStage)
        {
            LogContext.PushProperty("ActivityName", taskStage);
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnGetAsync));

            _logger.LogInformation("Entering {PortalResource} for _EditDatabase with: ProcessId: {ProcessId}; ActivityName: {ActivityName};");

            await GetCarisData(processId, taskStage);

            SessionFilename = _generalConfig.Value.SessionFilename;
        }

        public async Task<JsonResult> OnGetWorkspacesAsync()
        {
            var cachedHpdWorkspaces = await _dbContext.CachedHpdWorkspace.Select(c => c.Name).ToListAsync();
            return new JsonResult(cachedHpdWorkspaces);
        }

        public async Task<IActionResult> OnGetLaunchSourceEditorAsync(int processId, string taskStage, string sessionFilename)
        {
            LogContext.PushProperty("ActivityName", taskStage);
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnGetLaunchSourceEditorAsync));

            _logger.LogInformation("Launching Source Editor with: ProcessId: {ProcessId}; ActivityName: {ActivityName};");

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);
            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(processId, UserFullName, taskStage);

            var serializer = new XmlSerializer(typeof(SessionFile));

            var fs = new MemoryStream();
            try
            {
                serializer.Serialize(fs, sessionFile);

                fs.Position = 0;

                return File(fs, MediaTypeNames.Application.Octet, sessionFilename);
            }
            catch (InvalidOperationException ex)
            {
                fs.Dispose();
                _logger.LogError(ex, "Failed to serialize Caris session file.");
                throw;
            }
            catch (Exception ex)
            {
                fs.Dispose();
                _logger.LogError(ex, "Failed to generate session file.");
                throw;
            }
        }

        public async Task<IActionResult> OnPostCreateCarisProjectAsync(int processId, string taskStage, string projectName, string carisWorkspace)
        {
            HpdUser hpdUser;
            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            try
            {
                hpdUser = await _dbContext.HpdUser.SingleAsync(u => u.AdUsername.Equals(UserFullName,
                    StringComparison.CurrentCultureIgnoreCase));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserFullName}.");
                throw new InvalidOperationException($"Unable to find HPD username for {UserFullName}.",
                    ex.InnerException);
            }

            if (!await _dbContext.CachedHpdWorkspace.AnyAsync(c => c.Name.Equals(carisWorkspace, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogError($"Current Caris Workspace {carisWorkspace} is invalid.");
                throw new InvalidOperationException($"Current Caris Workspace {carisWorkspace} is invalid.");
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Please provide a Caris Project Name.");
            }

            var projectId = await _carisProjectHelper.CreateCarisProject(processId, projectName, hpdUser.HpdUsername,
                 null, _generalConfig.Value.CarisNewProjectType, _generalConfig.Value.CarisNewProjectStatus,
                 _generalConfig.Value.CarisNewProjectPriority, _generalConfig.Value.CarisProjectTimeoutSeconds, carisWorkspace);

            return StatusCode(200);
        }

        private async Task GetCarisData(int processId, string taskStage)
        {
            switch (taskStage)
            {
                case "Assess":
                    var assessData = await _dbContext.DbAssessmentAssessData.FirstAsync(ad => ad.ProcessId == processId);
                    SelectedCarisWorkspace = assessData.WorkspaceAffected;
                    ProjectName = assessData.CarisProjectName;
                    break;
                case "Verify":
                    var verifyData = await _dbContext.DbAssessmentVerifyData.FirstAsync(vd => vd.ProcessId == processId);
                    SelectedCarisWorkspace = verifyData.WorkspaceAffected;
                    ProjectName = verifyData.CarisProjectName;
                    break;
                default:
                    _logger.LogError("{ActivityName} is not implemented for processId: {ProcessId}.");
                    throw new NotImplementedException($"{taskStage} is not implemented for processId: {processId}.");
            }
        }

    }
}