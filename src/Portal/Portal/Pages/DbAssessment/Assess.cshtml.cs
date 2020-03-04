using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Auth;
using Portal.Configuration;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Models;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    [Authorize]
    public class AssessModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly ILogger<AssessModel> _logger;
        private readonly ICommentsHelper _commentsHelper;
        private readonly IUserIdentityService _userIdentityService;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;

        public bool IsOnHold { get; set; }
        public int ProcessId { get; set; }

        [BindProperty]
        public string Ion { get; set; }

        [BindProperty]
        public string ActivityCode { get; set; }

        [BindProperty]
        public string SourceCategory { get; set; }

        [BindProperty]
        public string TaskType { get; set; }

        [BindProperty]
        public string Assessor { get; set; }

        [BindProperty]
        public string Verifier { get; set; }

        public _OperatorsModel OperatorsModel { get; set; }

        [BindProperty]
        public List<ProductAction> RecordProductAction { get; set; }

        [BindProperty]
        public bool ProductActioned { get; set; }

        [BindProperty]
        public string ProductActionChangeDetails { get; set; }

        [BindProperty]
        public string SelectedCarisWorkspace { get; set; }

        [BindProperty]
        public string ProjectName { get; set; }

        public WorkflowStage WorkflowStage { get; set; }

        [BindProperty]
        public List<DataImpact> DataImpacts { get; set; }

        [BindProperty]
        public string Team { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public AssessModel(WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            ILogger<AssessModel> logger,
            ICommentsHelper commentsHelper,
            IUserIdentityService userIdentityService,
            IPageValidationHelper pageValidationHelper,
            ICarisProjectHelper carisProjectHelper,
            IOptions<GeneralConfig> generalConfig)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _logger = logger;
            _commentsHelper = commentsHelper;
            _userIdentityService = userIdentityService;
            _pageValidationHelper = pageValidationHelper;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            //TODO: Read operators from DB

            ProcessId = processId;

            var currentAssessData = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);
            OperatorsModel = _OperatorsModel.GetOperatorsData(currentAssessData);
            OperatorsModel.ParentPage = WorkflowStage = WorkflowStage.Assess;
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            LogContext.PushProperty("ActivityName", "Assess");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("Action", action);

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            LogContext.PushProperty("UserFullName", UserFullName);

            _logger.LogInformation("Entering Done with: ProcessId: {ProcessId}; ActivityName: {ActivityName}; Action: {Action};");

            ValidationErrorMessages.Clear();

            if (!await _pageValidationHelper.ValidateAssessPage(
                                                Ion,
                                                ActivityCode,
                                                SourceCategory,
                                                TaskType,
                                                Assessor,
                                                Verifier,
                                                RecordProductAction,
                                                DataImpacts,
                                                ValidationErrorMessages, Team))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            ProcessId = processId;

            await UpdateTaskInformation(processId);

            await UpdateProductAction(processId);

            await UpdateAssessmentData(processId);

            try
            {

                await UpdateEditDatabase(processId);

            }
            catch (Exception e)
            {
                ValidationErrorMessages.Add(e.Message);

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            await UpdateDataImpact(processId);

            if (action == "Done")
            {
                var workflowInstance = await _dbContext.WorkflowInstance
                    .Include(w => w.PrimaryDocumentStatus)
                    .FirstAsync(w => w.ProcessId == processId);

                workflowInstance.Status = WorkflowStatus.Updating.ToString();

                await _dbContext.SaveChangesAsync();

                var success = await _workflowServiceApiClient.ProgressWorkflowInstance(workflowInstance.ProcessId, workflowInstance.SerialNumber, "Assess", "Verify");

                if (success)
                {
                    await PersistCompletedAssess(processId, workflowInstance);

                    UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

                    LogContext.PushProperty("UserFullName", UserFullName);

                    _logger.LogInformation("{UserFullName} successfully progressed {ActivityName} to Verify on 'Done' button with: ProcessId: {ProcessId}; Action: {Action};");

                    await _commentsHelper.AddComment($"Assess step completed",
                        processId,
                        workflowInstance.WorkflowInstanceId,
                        UserFullName);
                }
                else
                {
                    workflowInstance.Status = WorkflowStatus.Started.ToString();
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Unable to progress task {ProcessId} from Assess to Verify.");

                    ValidationErrorMessages.Add("Unable to progress task from Assess to Verify. Please retry later.");

                    return new JsonResult(this.ValidationErrorMessages)
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }
            }

            _logger.LogInformation("Finished Done with: ProcessId: {ProcessId}; Action: {Action};");


            return StatusCode((int)HttpStatusCode.OK);
        }

        private async Task PersistCompletedAssess(int processId, WorkflowInstance workflowInstance)
        {
            var correlationId = workflowInstance.PrimaryDocumentStatus.CorrelationId;

            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = correlationId.HasValue ? correlationId.Value : Guid.NewGuid(),
                ProcessId = processId,
                FromActivityName = "Assess",
                ToActivityName = "Verify"
            };

            LogContext.PushProperty("PersistWorkflowInstanceDataEvent",
                persistWorkflowInstanceDataEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
            await _eventServiceApiClient.PostEvent(nameof(PersistWorkflowInstanceDataEvent), persistWorkflowInstanceDataEvent);
            _logger.LogInformation("Published PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
        }

        private async Task UpdateTaskInformation(int processId)
        {
            var currentAssess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);

            currentAssess.Verifier = Verifier;
            currentAssess.Ion = Ion;
            currentAssess.ActivityCode = ActivityCode;
            currentAssess.SourceCategory = SourceCategory;
            currentAssess.TaskType = TaskType;

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateAssessmentData(int processId)
        {
            var currentAssessment = await _dbContext.AssessmentData.FirstAsync(r => r.ProcessId == processId);
            currentAssessment.TeamDistributedTo = Team;

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateProductAction(int processId)
        {
            var currentAssess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);

            currentAssess.ProductActioned = ProductActioned;
            currentAssess.ProductActionChangeDetails = ProductActionChangeDetails;

            var toRemove = await _dbContext.ProductAction.Where(at => at.ProcessId == processId).ToListAsync();
            _dbContext.ProductAction.RemoveRange(toRemove);

            foreach (var productAction in RecordProductAction)
            {
                productAction.ProcessId = processId;
                _dbContext.ProductAction.Add(productAction);
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateEditDatabase(int processId)
        {
            var currentAssess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);

            var carisProjectDetails = await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);
            var isCarisProjectCreated = carisProjectDetails != null;

            if (isCarisProjectCreated)
            {
                // just update Caris project Assigned users
                await UpdateCarisProjectWithAdditionalUser(processId, currentAssess.Assessor);
                await UpdateCarisProjectWithAdditionalUser(processId, currentAssess.Verifier);

                return;
            }

            currentAssess.WorkspaceAffected = SelectedCarisWorkspace;
            await _dbContext.SaveChangesAsync();
        }

        private async Task UpdateCarisProjectWithAdditionalUser(int processId, string userName)
        {

            var hpdUsername = await GetHpdUser(userName);  // which will also implicitly validate if the other user has been mapped to HPD account in our database
            var carisProjectDetails =
                await _dbContext.CarisProjectDetails.SingleOrDefaultAsync(cp => cp.ProcessId == processId);

            LogContext.PushProperty("CarisProjectId", carisProjectDetails.ProjectId);
            LogContext.PushProperty("HpdUsername", hpdUsername.HpdUsername);

            try
            {
                await _carisProjectHelper.UpdateCarisProject(carisProjectDetails.ProjectId, hpdUsername.HpdUsername,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to assign {userName} ({hpdUsername.HpdUsername}) to Caris project: {carisProjectDetails.ProjectId}");
                throw new InvalidOperationException($"Edit Database: Failed to assign {userName} ({hpdUsername.HpdUsername}) to Caris project: {carisProjectDetails.ProjectId}. {e.Message}");
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
                throw new InvalidOperationException($"Edit Database: Unable to find HPD username for {username} in our system.",
                    ex.InnerException);
            }

        }
        
        private async Task UpdateDataImpact(int processId)
        {
            var toRemove = await _dbContext.DataImpact.Where(at => at.ProcessId == processId).ToListAsync();
            _dbContext.DataImpact.RemoveRange(toRemove);

            if (DataImpacts.Any(a => a.HpdUsageId > 0))
            {
                foreach (var dataImpact in DataImpacts)
                {
                    if (dataImpact.HpdUsageId > 0)
                    {
                        dataImpact.ProcessId = processId;
                        _dbContext.DataImpact.Add(dataImpact);
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task UpdateSdraAssessmentAsCompleted(string comment, WorkflowInstance workflowInstance)
        {
            try
            {
                await _dataServiceApiClient.PutAssessmentCompleted(workflowInstance.AssessmentData.PrimarySdocId,
                    comment);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed requesting DataService {DataServiceResource} with: PrimarySdocId: {PrimarySdocId}; Comment: {Comment};",
                    nameof(_dataServiceApiClient.PutAssessmentCompleted),
                    workflowInstance.AssessmentData.PrimarySdocId,
                    comment);
            }
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}