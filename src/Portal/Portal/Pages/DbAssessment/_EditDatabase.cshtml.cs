﻿using System;
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

        public CarisProjectDetails CarisProjectDetails { get; set; }

        public bool IsCarisProjectCreated { get; set; }

        private string _userFullName;

        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
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

            await GetCarisProjectDetails(processId);

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

            LogContext.PushProperty("UserFullName", UserFullName);

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

        public async Task<IActionResult> OnPostCreateCarisProjectAsync(int processId, string taskStage, string projectName,
            string carisWorkspace)
        {
            LogContext.PushProperty("ActivityName", taskStage);
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostCreateCarisProjectAsync));
            LogContext.PushProperty("ProjectName", projectName);
            LogContext.PushProperty("CarisWorkspace", carisWorkspace);

            _logger.LogInformation("Entering {PortalResource} for _EditDatabase with: ProcessId: {ProcessId}; ActivityName: {ActivityName};");

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            LogContext.PushProperty("UserFullName", UserFullName);

            await ValidateCarisProjectDetails(processId, projectName, carisWorkspace, taskStage, UserFullName);

            var projectId = await CreateCarisProject(processId, projectName, carisWorkspace);

            await UpdateCarisProjectDetails(processId, projectName, projectId);

            // Add assessor and verifier to created project
            await UpdateCarisProjectWithAdditionalUser(projectId, processId, taskStage);

            return StatusCode(200);
        }

        private async Task<int> CreateCarisProject(int processId, string projectName, string carisWorkspace)
        {

            var carisProjectDetails = await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);

            if (carisProjectDetails != null)
            {
                return carisProjectDetails.ProjectId;
            }

            // which will also implicitly validate if the current user has been mapped to HPD account in our database
            var hpdUser = await GetHpdUser(UserFullName);

            _logger.LogInformation(
                "Creating Caris Project with ProcessId: {ProcessId}; ProjectName: {ProjectName}; CarisWorkspace {CarisWorkspace}.");

            var projectId = await _carisProjectHelper.CreateCarisProject(processId, projectName,
                hpdUser.HpdUsername, _generalConfig.Value.CarisNewProjectType,
                _generalConfig.Value.CarisNewProjectStatus,
                _generalConfig.Value.CarisNewProjectPriority, _generalConfig.Value.CarisProjectTimeoutSeconds,
                carisWorkspace);

            return projectId;
        }

        private async Task UpdateCarisProjectDetails(int processId, string projectName, int projectId)
        {
            // If somehow the user has already created a project, remove it and create new row
            var toRemove = await _dbContext.CarisProjectDetails.Where(cp => cp.ProcessId == processId).ToListAsync();
            if (toRemove.Any())
            {
                _dbContext.CarisProjectDetails.RemoveRange(toRemove);
                await _dbContext.SaveChangesAsync();
            }

            _dbContext.CarisProjectDetails.Add(new CarisProjectDetails
            {
                ProcessId = processId,
                Created = DateTime.Now,
                CreatedBy = UserFullName,
                ProjectId = projectId,
                ProjectName = projectName
            });

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateCarisProjectWithAdditionalUser(int projectId, int processId, string taskStage)
        {
            var additionalUsername = await GetAdditionalUserToAssignedToCarisproject(processId, taskStage);
            var hpdUsername = await GetHpdUser(additionalUsername);  // which will also implicitly validate if the other user has been mapped to HPD account in our database

            try
            {
                await _carisProjectHelper.UpdateCarisProject(projectId, hpdUsername.HpdUsername,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Project created but failed to add {hpdUsername.HpdUsername} as an Assigned-to user to Caris project: {projectId}");
                throw new InvalidOperationException($"Project created but failed to add {hpdUsername.HpdUsername} as an Assigned-to user to Caris project: {projectId}. {e.Message}");
            }
        }

        private async Task ValidateCarisProjectDetails(int processId, string projectName, string carisWorkspace, string taskStage, string currentLoggedInUser)
        {
            var userAssignedToTask = await GetUserAssignedToTask(processId, taskStage);

            if (!userAssignedToTask.Equals(currentLoggedInUser, StringComparison.InvariantCultureIgnoreCase))
            {
                LogContext.PushProperty("UserAssignedToTask", userAssignedToTask);
                _logger.LogError("{UserFullName} is not assigned to this task with processId {ProcessId}, {UserAssignedToTask} is assigned to this task.");
                throw new InvalidOperationException($"{currentLoggedInUser} is not assigned to this task with processId {processId}, {userAssignedToTask} is assigned to this task.");
            }

            if (!await _dbContext.CachedHpdWorkspace.AnyAsync(c =>
                c.Name.Equals(carisWorkspace, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogError($"Current Caris Workspace {carisWorkspace} is invalid.");
                throw new InvalidOperationException($"Current Caris Workspace {carisWorkspace} is invalid.");
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Please provide a Caris Project Name.");
            }
        }

        private async Task<string> GetUserAssignedToTask(int processId, string taskStage)
        {
            switch (taskStage)
            {
                case "Assess":
                    var assessData = await _dbContext.DbAssessmentAssessData.FirstAsync(ad => ad.ProcessId == processId);
                    return assessData.Assessor;
                case "Verify":
                    var verifyData = await _dbContext.DbAssessmentVerifyData.FirstAsync(vd => vd.ProcessId == processId);
                    return verifyData.Verifier;
                default:
                    _logger.LogError("{ActivityName} is not implemented for processId: {ProcessId}.");
                    throw new NotImplementedException($"{taskStage} is not implemented for processId: {processId}.");
            }
        }

        private async Task<string> GetAdditionalUserToAssignedToCarisproject(int processId, string taskStage)
        {
            switch (taskStage)
            {
                case "Assess":
                    // at Assess stage; Assessor will be current user; so only use Verifier as the additional 'assigned to'

                    var assessData = await _dbContext.DbAssessmentAssessData.FirstAsync(ad => ad.ProcessId == processId);
                    return assessData.Verifier;
                case "Verify":
                    // at Verify stage; Verifier will be current user; so only use Assessor as the additional 'assigned to'

                    var verifyData = await _dbContext.DbAssessmentVerifyData.FirstAsync(vd => vd.ProcessId == processId);
                    return verifyData.Assessor;
                default:
                    _logger.LogError("{ActivityName} is not implemented for processId: {ProcessId}.");
                    throw new NotImplementedException($"{taskStage} is not implemented for processId: {processId}.");
            }
        }

        private async Task<HpdUser> GetHpdUser(string username)
        {
            try
            {
                return await _dbContext.HpdUser.SingleAsync(u => u.AdUsername.Equals(username,
                    StringComparison.InvariantCultureIgnoreCase));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserFullName} in our system.");
                throw new InvalidOperationException($"Unable to find HPD username for {username}  in our system.",
                    ex.InnerException);
            }

        }

        private async Task GetCarisData(int processId, string taskStage)
        {
            switch (taskStage)
            {
                case "Assess":
                    var assessData = await _dbContext.DbAssessmentAssessData.FirstAsync(ad => ad.ProcessId == processId);
                    SelectedCarisWorkspace = assessData.WorkspaceAffected;
                    break;
                case "Verify":
                    var verifyData = await _dbContext.DbAssessmentVerifyData.FirstAsync(vd => vd.ProcessId == processId);
                    SelectedCarisWorkspace = verifyData.WorkspaceAffected;
                    break;
                default:
                    _logger.LogError("{ActivityName} is not implemented for processId: {ProcessId}.");
                    throw new NotImplementedException($"{taskStage} is not implemented for processId: {processId}.");
            }
        }

        private async Task GetCarisProjectDetails(int processId)
        {
            CarisProjectDetails = await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);

            IsCarisProjectCreated = CarisProjectDetails != null;
            ProjectName = CarisProjectDetails != null ? CarisProjectDetails.ProjectName : "";
        }

        private async Task<bool> CheckCarisProjectCreated(int processId)
        {
            CarisProjectDetails = await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);

            return CarisProjectDetails != null;
        }

    }
}