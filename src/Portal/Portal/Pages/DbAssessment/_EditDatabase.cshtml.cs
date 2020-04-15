using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Common.Helpers;
using Common.Helpers.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Portal.Helpers;
using Portal.Models;
using Portal.ViewModels;
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
        private IAdDirectoryService _adDirectoryService;
        private readonly ISessionFileGenerator _sessionFileGenerator;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly ICarisProjectNameGenerator _carisProjectNameGenerator;

        [BindProperty(SupportsGet = true)]
        [DisplayName("Select CARIS Workspace:")]
        public string SelectedCarisWorkspace { get; set; }

        [BindProperty(SupportsGet = true)]
        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        public string SessionFilename { get; set; }

        public CarisProjectDetails CarisProjectDetails { get; set; }

        public bool IsCarisProjectCreated { get; set; }

        public int CarisProjectNameCharacterLimit { get; set; }

        public int UsagesSelectionPageLength { get; set; }

        public int SourcesSelectionPageLength { get; set; }

        public List<string> HpdUsages { get; set; }

        public List<SourceViewModel> SourceDocuments { get; set; } = new List<SourceViewModel>();

        private string _userFullName;

        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public _EditDatabaseModel(WorkflowDbContext dbContext, ILogger<_EditDatabaseModel> logger,
            IOptions<GeneralConfig> generalConfig,
            IAdDirectoryService adDirectoryService,
            ISessionFileGenerator sessionFileGenerator,
            ICarisProjectHelper carisProjectHelper,
            ICarisProjectNameGenerator carisProjectNameGenerator)
        {
            _dbContext = dbContext;
            _logger = logger;
            _generalConfig = generalConfig;
            _adDirectoryService = adDirectoryService;
            _sessionFileGenerator = sessionFileGenerator;
            _carisProjectHelper = carisProjectHelper;
            _carisProjectNameGenerator = carisProjectNameGenerator;
        }

        public async Task OnGetAsync(int processId, string taskStage)
        {
            LogContext.PushProperty("ActivityName", taskStage);
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnGetAsync));

            _logger.LogInformation("Entering {PortalResource} for _EditDatabase with: ProcessId: {ProcessId}; ActivityName: {ActivityName};");

            HpdUsages = await _dbContext.HpdUsage.Select(u => u.Name).ToListAsync();

            await GetSourceDocuments(processId);

            await GetCarisData(processId, taskStage);

            await GetCarisProjectDetails(processId, taskStage);

            UsagesSelectionPageLength = _generalConfig.Value.UsagesSelectionPageLength;
            SourcesSelectionPageLength = _generalConfig.Value.SourcesSelectionPageLength;
            CarisProjectNameCharacterLimit = _generalConfig.Value.CarisProjectNameCharacterLimit;
            SessionFilename = _generalConfig.Value.SessionFilename;
        }

        private async Task GetSourceDocuments(int processId)
        {
            var assessmentData = await _dbContext.AssessmentData
                .FirstOrDefaultAsync(ad => ad.ProcessId == processId);
            var primaryDocumentStatus = await _dbContext.PrimaryDocumentStatus
                .FirstOrDefaultAsync(pds => pds.ProcessId == processId);

            if (assessmentData != null && primaryDocumentStatus != null &&
                primaryDocumentStatus.Status == SourceDocumentRetrievalStatus.FileGenerated.ToString())
            {
                var primarySourceDocument = new SourceViewModel()
                {
                    DocumentName = assessmentData.SourceDocumentName,
                    FileExtension = "Not implemented",
                    Path = _generalConfig.Value.SourceDocumentWriteableFolderName
                };

                SourceDocuments.Add(primarySourceDocument);
            }

            var linkedDocuments = await _dbContext.LinkedDocument
                .Where(ld => ld.ProcessId == processId &&
                                          ld.Status == SourceDocumentRetrievalStatus.FileGenerated.ToString())
                .Select(ld => new SourceViewModel()
                {
                    DocumentName = ld.SourceDocumentName,
                    FileExtension = "Not implemented",
                    Path = _generalConfig.Value.SourceDocumentWriteableFolderName
                })
                .ToListAsync();

            SourceDocuments.AddRange(linkedDocuments);

            var databaseDocuments = await _dbContext.DatabaseDocumentStatus
                .Where(dd => dd.ProcessId == processId &&
                                                dd.Status == SourceDocumentRetrievalStatus.FileGenerated.ToString())
                .Select(dd => new SourceViewModel()
                {
                    DocumentName = dd.SourceDocumentName,
                    FileExtension = "Not implemented",
                    Path = _generalConfig.Value.SourceDocumentWriteableFolderName
                })
                .ToListAsync();

            SourceDocuments.AddRange(databaseDocuments);
        }

        public async Task<JsonResult> OnGetWorkspacesAsync()
        {
            var cachedHpdWorkspaces = await _dbContext.CachedHpdWorkspace.Select(c => c.Name).ToListAsync();
            return new JsonResult(cachedHpdWorkspaces);
        }

        public async Task<IActionResult> OnGetLaunchSourceEditorAsync(int processId, string taskStage, string sessionFilename,
                                                                        List<string> selectedHpdUsages, List<string> selectedSources)
        {
            LogContext.PushProperty("ActivityName", taskStage);
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnGetLaunchSourceEditorAsync));
            LogContext.PushProperty("SelectedHpdUsages", (selectedHpdUsages != null && selectedHpdUsages.Count > 0 ? string.Join(',', selectedHpdUsages) : ""));
            LogContext.PushProperty("SelectedSources", (selectedSources != null && selectedSources.Count > 0 ? string.Join(',', selectedSources) : ""));

            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

            LogContext.PushProperty("UserFullName", UserFullName);

            _logger.LogInformation("Entering {PortalResource} for _EditDatabase with: ProcessId: {ProcessId}; " +
                                   "ActivityName: {ActivityName}; " +
                                   "with SelectedHpdUsages {SelectedHpdUsages}, " +
                                   "and SelectedSources {SelectedSources}");

            if (selectedHpdUsages == null || selectedHpdUsages.Count == 0)
            {
                _logger.LogError("Failed to generate session file. No Hpd Usages were selected. " +
                                 "ProcessId: {ProcessId}; " +
                                 "ActivityName: {ActivityName};");
                throw new ArgumentException("Failed to generate session file. No Hpd Usages were selected.");
            }

            var carisProjectDetails = await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);

            var isCarisProjectCreated = carisProjectDetails != null;

            if (!isCarisProjectCreated)
            {
                _logger.LogError("Failed to generate session file. Caris project was never created. " +
                                 "ProcessId: {ProcessId}; " +
                                 "ActivityName: {ActivityName};");
                throw new ArgumentException("Failed to generate session file. Caris project was never created.");
            }

            var sessionFile = await _sessionFileGenerator.PopulateSessionFile(
                                                                                processId,
                                                                                UserFullName,
                                                                                taskStage,
                                                                                carisProjectDetails,
                                                                                selectedHpdUsages,
                                                                                selectedSources);

            var serializer = new XmlSerializer(typeof(SessionFile));

            var fs = new MemoryStream();
            try
            {

                var xmlnsEmpty = new XmlSerializerNamespaces();
                xmlnsEmpty.Add("", "");
                serializer.Serialize(fs, sessionFile, xmlnsEmpty);

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

            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

            LogContext.PushProperty("UserFullName", UserFullName);

            await ValidateCarisProjectDetails(processId, projectName, carisWorkspace, taskStage, UserFullName);

            var projectId = await CreateCarisProject(processId, projectName);

            await UpdateCarisProjectDetails(processId, projectName, projectId);

            // Add assessor and verifier to created project
            await UpdateCarisProjectWithAdditionalUser(projectId, processId, taskStage);

            var assessData = await _dbContext.DbAssessmentAssessData.FirstAsync(ad => ad.ProcessId == processId);
            assessData.WorkspaceAffected = carisWorkspace;
            await _dbContext.SaveChangesAsync();

            return StatusCode(200);
        }

        private async Task<int> CreateCarisProject(int processId, string projectName)
        {

            var carisProjectDetails = await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);

            if (carisProjectDetails != null)
            {
                return carisProjectDetails.ProjectId;
            }

            // which will also implicitly validate if the current user has been mapped to HPD account in our database
            var hpdUser = await GetHpdUser(UserFullName);

            _logger.LogInformation(
                "Creating Caris Project with ProcessId: {ProcessId}; ProjectName: {ProjectName}.");

            var projectId = await _carisProjectHelper.CreateCarisProject(processId, projectName,
                hpdUser.HpdUsername, _generalConfig.Value.CarisNewProjectType,
                _generalConfig.Value.CarisNewProjectStatus,
                _generalConfig.Value.CarisNewProjectPriority, _generalConfig.Value.CarisProjectTimeoutSeconds);

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
                _logger.LogError(e, $"Project created but failed to assign {additionalUsername} ({hpdUsername.HpdUsername}) to Caris project: {projectId}");
                throw new InvalidOperationException($"Project created but failed to assign {additionalUsername} ({hpdUsername.HpdUsername}) to Caris project: {projectId}. {e.Message}");
            }
        }

        private async Task ValidateCarisProjectDetails(int processId, string projectName, string carisWorkspace, string taskStage, string currentLoggedInUser)
        {
            var userAssignedToTask = await GetUserAssignedToTask(processId, taskStage);

            if (!userAssignedToTask.Equals(currentLoggedInUser, StringComparison.InvariantCultureIgnoreCase))
            {
                LogContext.PushProperty("UserAssignedToTask", userAssignedToTask);
                _logger.LogError("{UserFullName} is not assigned to this task with processId {ProcessId}, {UserAssignedToTask} is assigned to this task.");
                throw new InvalidOperationException($"{userAssignedToTask} is assigned to this task. Please assign the task to yourself and click Save");
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

        private async Task GetCarisProjectDetails(int processId, string taskStage)
        {
            CarisProjectDetails = await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);

            IsCarisProjectCreated = CarisProjectDetails != null;

            if (!IsCarisProjectCreated)
            {
                if (taskStage == "Assess")
                {
                    var assessmentData =
                        await _dbContext.AssessmentData.SingleAsync(ad => ad.ProcessId == processId);
                    var parsedRsdraNumber = assessmentData.ParsedRsdraNumber;
                    var sourceDocumentName = assessmentData.SourceDocumentName;

                    ProjectName = _carisProjectNameGenerator.Generate(processId, parsedRsdraNumber, sourceDocumentName);
                    return;
                }

                ProjectName = "NO PROJECT WAS CREATED AT ASSESS";
                return;
            }

            ProjectName = CarisProjectDetails.ProjectName;
        }
    }
}