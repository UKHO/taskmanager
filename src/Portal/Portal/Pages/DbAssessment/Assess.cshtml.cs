using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Helpers.Auth;
using Common.Messages.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Auth;
using Portal.Configuration;
using Portal.Extensions;
using Portal.Helpers;
using Portal.HttpClients;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    [Authorize]
    public class AssessModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly ILogger<AssessModel> _logger;
        private readonly ICommentsHelper _dbAssessmentCommentsHelper;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IPortalUserDbService _portalUserDbService;

        [BindProperty]
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
        public AdUser Assessor { get; set; }

        [BindProperty]
        public AdUser Verifier { get; set; }

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
        public DataImpact StsDataImpact { get; set; }

        [BindProperty]
        public string Team { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        public string SerialisedCustomHttpStatusCodes { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;
        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        public AssessModel(WorkflowDbContext dbContext,
            IEventServiceApiClient eventServiceApiClient,
            ILogger<AssessModel> logger,
            ICommentsHelper dbAssessmentCommentsHelper,
            IAdDirectoryService adDirectoryService,
            IPageValidationHelper pageValidationHelper,
            ICarisProjectHelper carisProjectHelper,
            IOptions<GeneralConfig> generalConfig,
            IPortalUserDbService portalUserDbService)
        {
            _dbContext = dbContext;
            _eventServiceApiClient = eventServiceApiClient;
            _logger = logger;
            _dbAssessmentCommentsHelper = dbAssessmentCommentsHelper;
            _adDirectoryService = adDirectoryService;
            _pageValidationHelper = pageValidationHelper;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;
            _portalUserDbService = portalUserDbService;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;

            var currentAssessData = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);
            OperatorsModel = await _OperatorsModel.GetOperatorsDataAsync(currentAssessData, _dbContext).ConfigureAwait(false);
            OperatorsModel.ParentPage = WorkflowStage = WorkflowStage.Assess;
            SerialisedCustomHttpStatusCodes = EnumHandlers.EnumToString<AssessCustomHttpStatusCode>();

            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostSaveAsync(int processId)
        {
            LogContext.PushProperty("ActivityName", "Assess");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostSaveAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            var action = "Save";
            LogContext.PushProperty("Action", action);

            _logger.LogInformation("Entering Save with: ProcessId: {ProcessId}; ActivityName: {ActivityName}; Action: {Action};");

            ValidationErrorMessages.Clear();

            var isUserValid = await _portalUserDbService.ValidateUserAsync(CurrentUser.UserPrincipalName);

            if (!isUserValid)
            {
                ValidationErrorMessages.Add("Operators: Your user account cannot be accepted. Please contact system administrators");

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)AssessCustomHttpStatusCode.FailedValidation
                };
            }

            ProcessId = processId;

            var currentAssessData = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);

            if (!await _pageValidationHelper.CheckAssessPageForErrors(
                action,
                Ion,
                ActivityCode,
                SourceCategory,
                TaskType,
                ProductActioned,
                ProductActionChangeDetails,
                RecordProductAction,
                DataImpacts, StsDataImpact, Team, Assessor, Verifier, ValidationErrorMessages, CurrentUser.UserPrincipalName, currentAssessData.Assessor))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)AssessCustomHttpStatusCode.FailedValidation
                };
            }

            if (!await SaveTaskData(processId, currentAssessData.WorkflowInstanceId))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)AssessCustomHttpStatusCode.FailuresDetected
                };
            }

            _logger.LogInformation("Finished Save with: ProcessId: {ProcessId}; Action: {Action};");

            return StatusCode((int)HttpStatusCode.OK);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            LogContext.PushProperty("ActivityName", "Assess");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("Action", action);
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            _logger.LogInformation("Entering Done with: ProcessId: {ProcessId}; ActivityName: {ActivityName}; Action: {Action};");

            ValidationErrorMessages.Clear();

            var isUserValid = await _portalUserDbService.ValidateUserAsync(CurrentUser.UserPrincipalName);

            if (!isUserValid)
            {
                ValidationErrorMessages.Add("Operators: Your user account cannot be accepted. Please contact system administrators");

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)AssessCustomHttpStatusCode.FailedValidation
                };
            }

            ProcessId = processId;

            var currentAssessData = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);

            switch (action)
            {
                case "Done":
                    if (!await _pageValidationHelper.CheckAssessPageForErrors(
                        action,
                        Ion,
                        ActivityCode,
                        SourceCategory,
                        TaskType,
                        ProductActioned,
                        ProductActionChangeDetails,
                        RecordProductAction,
                        DataImpacts, StsDataImpact, Team, Assessor, Verifier, ValidationErrorMessages, CurrentUser.UserPrincipalName, currentAssessData.Assessor))
                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)AssessCustomHttpStatusCode.FailedValidation
                        };
                    }

                    var hasWarnings = _pageValidationHelper.CheckAssessPageForWarnings(action, DataImpacts, StsDataImpact, ValidationErrorMessages);

                    if (hasWarnings)
                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)AssessCustomHttpStatusCode.WarningsDetected
                        };

                    }

                    try
                    {
                        await MarkTaskAsComplete(processId);
                    }
                    catch (Exception e)
                    {
                        await MarkWorkflowInstanceAsStarted(processId);

                        _logger.LogError("Unable to progress task {ProcessId} from Assess to Verify.", e);

                        ValidationErrorMessages.Add($"Unable to progress task from Assess to Verify. Please retry later: {e.Message}");

                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)AssessCustomHttpStatusCode.FailuresDetected
                        };
                    }

                    break;
                case "ConfirmedDone":

                    try
                    {
                        await MarkTaskAsComplete(processId);
                    }
                    catch (Exception e)
                    {
                        await MarkWorkflowInstanceAsStarted(processId);

                        _logger.LogError("Unable to progress task {ProcessId} from Assess to Verify.", e);

                        ValidationErrorMessages.Add($"Unable to progress task from Assess to Verify. Please retry later: {e.Message}");

                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)AssessCustomHttpStatusCode.FailuresDetected
                        };
                    }

                    break;
                default:
                    _logger.LogError("Action not found {Action}");

                    throw new NotImplementedException($"Action not found {action}");
            }

            _logger.LogInformation("Finished Done with: ProcessId: {ProcessId}; Action: {Action};");

            return StatusCode((int)HttpStatusCode.OK);
        }

        private async Task MarkTaskAsComplete(int processId)
        {
            var workflowInstance = await MarkWorkflowInstanceAsUpdating(processId);

            await PublishProgressWorkflowInstanceEvent(processId, workflowInstance);


            _logger.LogInformation(
                "Task progression from {ActivityName} to Verify has been triggered by {UserPrincipalName} with: ProcessId: {ProcessId}; Action: {Action};");

            await _dbAssessmentCommentsHelper.AddComment("Task progression from Assess to Verify has been triggered",
                                                                        processId,
                                                                    workflowInstance.WorkflowInstanceId,
                                                                     CurrentUser.UserPrincipalName);
        }

        private async Task PublishProgressWorkflowInstanceEvent(int processId, WorkflowInstance workflowInstance)
        {
            var correlationId = workflowInstance.PrimaryDocumentStatus.CorrelationId;

            var progressWorkflowInstanceEvent = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = correlationId.HasValue ? correlationId.Value : Guid.NewGuid(),
                ProcessId = processId,
                FromActivity = WorkflowStage.Assess,
                ToActivity = WorkflowStage.Verify
            };

            LogContext.PushProperty("ProgressWorkflowInstanceEvent",
                progressWorkflowInstanceEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing ProgressWorkflowInstanceEvent: {ProgressWorkflowInstanceEvent};");
            await _eventServiceApiClient.PostEvent(nameof(ProgressWorkflowInstanceEvent), progressWorkflowInstanceEvent);
            _logger.LogInformation("Published ProgressWorkflowInstanceEvent: {ProgressWorkflowInstanceEvent};");
        }

        private async Task<WorkflowInstance> MarkWorkflowInstanceAsUpdating(int processId)
        {
            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(w => w.PrimaryDocumentStatus)
                .FirstAsync(w => w.ProcessId == processId);

            workflowInstance.Status = WorkflowStatus.Updating.ToString();

            await _dbContext.SaveChangesAsync();
            return workflowInstance;
        }

        private async Task MarkWorkflowInstanceAsStarted(int processId)
        {
            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(w => w.PrimaryDocumentStatus)
                .FirstAsync(w => w.ProcessId == processId);

            workflowInstance.Status = WorkflowStatus.Started.ToString();

            await _dbContext.SaveChangesAsync();
        }

        private async Task<bool> SaveTaskData(int processId, int workflowInstanceId)
        {
            await UpdateOnHold(ProcessId, IsOnHold);
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

                return false;
            }

            await UpdateDataImpact(processId);

            await UpdateStsDataImpact(processId);
            await _dbAssessmentCommentsHelper.AddComment($"Assess: Changes saved",
                processId,
                workflowInstanceId,
                 CurrentUser.UserPrincipalName);


            return true;
        }

        private async Task UpdateTaskInformation(int processId)
        {
            var currentAssess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);

            currentAssess.Assessor = await _portalUserDbService.GetAdUserAsync(Assessor.UserPrincipalName);
            currentAssess.Verifier = await _portalUserDbService.GetAdUserAsync(Verifier.UserPrincipalName);
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

            if (RecordProductAction != null)
            {
                foreach (var productAction in RecordProductAction)
                {
                    productAction.ProcessId = processId;
                    _dbContext.ProductAction.Add(productAction);
                }
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

        private async Task UpdateCarisProjectWithAdditionalUser(int processId, AdUser user)
        {

            var hpdUsername = await GetHpdUser(user);  // which will also implicitly validate if the other user has been mapped to HPD account in our database
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
                _logger.LogError(e, $"Failed to assign {user.DisplayName} with UPN {user.UserPrincipalName} and HPD username ({hpdUsername.HpdUsername}) to Caris project: {carisProjectDetails.ProjectId}");
                throw new InvalidOperationException($"Edit Database: Failed to assign {user.DisplayName} - ({hpdUsername.HpdUsername}) to Caris project: {carisProjectDetails.ProjectId}. {e.Message}");
            }
        }

        private async Task<HpdUser> GetHpdUser(AdUser user)
        {
            try
            {
                return await _dbContext.HpdUser.SingleAsync(u => u.AdUser.UserPrincipalName == user.UserPrincipalName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserPrincipalName} in our system.");
                throw new InvalidOperationException($"Edit Database: Unable to find HPD username for {user.DisplayName} in our system.",
                    ex.InnerException);
            }

        }

        private async Task UpdateDataImpact(int processId)
        {
            var toRemove = await _dbContext.DataImpact
                .Where(at => at.ProcessId == processId && !at.StsUsage)
                .ToListAsync();
            _dbContext.DataImpact.RemoveRange(toRemove);

            if (DataImpacts.Any(a => a.HpdUsageId > 0))
            {
                foreach (var dataImpact in DataImpacts)
                {
                    if (dataImpact.HpdUsageId > 0)
                    {
                        dataImpact.ProcessId = processId;
                        dataImpact.StsUsage = false;
                        _dbContext.DataImpact.Add(dataImpact);
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task UpdateStsDataImpact(int processId)
        {
            var toRemove = await _dbContext.DataImpact
                .Where(at => at.ProcessId == processId && at.StsUsage)
                .ToListAsync();
            _dbContext.DataImpact.RemoveRange(toRemove);

            if (StsDataImpact?.HpdUsageId > 0)
            {
                StsDataImpact.ProcessId = processId;
                StsDataImpact.Edited = false;
                StsDataImpact.FeaturesSubmitted = false;
                StsDataImpact.StsUsage = true;

                _dbContext.DataImpact.Add(StsDataImpact);

                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task UpdateOnHold(int processId, bool onHold)
        {
            if (onHold)
            {
                IsOnHold = true;

                var existingOnHoldRecord = await _dbContext.OnHold.FirstOrDefaultAsync(r => r.ProcessId == processId &&
                                                                                                         r.OffHoldTime == null);
                if (existingOnHoldRecord != null)
                {
                    return;
                }

                var workflowInstance = _dbContext.WorkflowInstance
                    .FirstOrDefault(wi => wi.ProcessId == processId);

                var onHoldRecord = new OnHold
                {
                    ProcessId = processId,
                    OnHoldTime = DateTime.Now,
                    OnHoldBy = await _portalUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName),
                    WorkflowInstanceId = workflowInstance.WorkflowInstanceId
                };

                await _dbContext.OnHold.AddAsync(onHoldRecord);
                await _dbContext.SaveChangesAsync();

                await _dbAssessmentCommentsHelper.AddComment($"Task {processId} has been put on hold",
                    processId,
                    workflowInstance.WorkflowInstanceId,
                     CurrentUser.UserPrincipalName);
            }
            else
            {
                IsOnHold = false;

                var existingOnHoldRecord = await _dbContext.OnHold.FirstOrDefaultAsync(r => r.ProcessId == processId &&
                                                                                                     r.OffHoldTime == null);
                if (existingOnHoldRecord == null)
                {
                    return;
                }

                existingOnHoldRecord.OffHoldTime = DateTime.Now;
                existingOnHoldRecord.OffHoldBy = await _portalUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

                await _dbContext.SaveChangesAsync();

                await _dbAssessmentCommentsHelper.AddComment($"Task {processId} taken off hold",
                    processId,
                    _dbContext.WorkflowInstance.First(p => p.ProcessId == processId)
                        .WorkflowInstanceId,
                     CurrentUser.UserPrincipalName);
            }
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}