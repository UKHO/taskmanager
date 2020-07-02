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
using Portal.BusinessLogic;
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
    public class VerifyModel : PageModel
    {
        private readonly ICommentsHelper _dbAssessmentCommentsHelper;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<VerifyModel> _logger;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IPortalUserDbService _portalUserDbService;
        private readonly WorkflowDbContext _dbContext;
        private readonly IWorkflowBusinessLogicService _workflowBusinessLogicService;
        private readonly IEventServiceApiClient _eventServiceApiClient;

        [BindProperty]
        public bool IsOnHold { get; set; }
        public int ProcessId { get; set; }
        public bool IsReadOnly { get; set; }
        public _OperatorsModel OperatorsModel { get; set; }

        [BindProperty]
        public List<ProductAction> RecordProductAction { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;
        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        public List<string> ValidationErrorMessages { get; set; }

        [BindProperty]
        public string Ion { get; set; }

        [BindProperty]
        public string ActivityCode { get; set; }

        [BindProperty]
        public string SourceCategory { get; set; }

        [BindProperty]
        public AdUser Verifier { get; set; }

        [BindProperty]
        public List<DataImpact> DataImpacts { get; set; }

        [BindProperty]
        public DataImpact StsDataImpact { get; set; }

        [BindProperty]
        public bool ProductActioned { get; set; }

        [BindProperty]
        public string ProductActionChangeDetails { get; set; }

        [BindProperty]
        public string SelectedCarisWorkspace { get; set; }

        public WorkflowStage WorkflowStage { get; set; }

        [BindProperty]
        public string ProjectName { get; set; }

        [BindProperty]
        public string Team { get; set; }

        public string SerialisedCustomHttpStatusCodes { get; set; }

        public VerifyModel(WorkflowDbContext dbContext,
            IWorkflowBusinessLogicService workflowBusinessLogicService,
            IEventServiceApiClient eventServiceApiClient,
            ICommentsHelper dbAssessmentCommentsHelper,
            IAdDirectoryService adDirectoryService,
            ILogger<VerifyModel> logger,
            IPageValidationHelper pageValidationHelper,
            ICarisProjectHelper carisProjectHelper,
            IOptions<GeneralConfig> generalConfig,
            IPortalUserDbService portalUserDbService)
        {
            _dbContext = dbContext;
            _workflowBusinessLogicService = workflowBusinessLogicService;
            _eventServiceApiClient = eventServiceApiClient;
            _dbAssessmentCommentsHelper = dbAssessmentCommentsHelper;
            _adDirectoryService = adDirectoryService;
            _logger = logger;
            _pageValidationHelper = pageValidationHelper;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;
            _portalUserDbService = portalUserDbService;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;

            IsReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(ProcessId);

            var currentVerifyData = await _dbContext.DbAssessmentVerifyData.AsNoTracking().FirstAsync(r => r.ProcessId == processId);
            OperatorsModel = await _OperatorsModel.GetOperatorsDataAsync(currentVerifyData, _dbContext).ConfigureAwait(false);
            OperatorsModel.ParentPage = WorkflowStage = WorkflowStage.Verify;
            SerialisedCustomHttpStatusCodes = EnumHandlers.EnumToString<VerifyCustomHttpStatusCode>();

            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostSaveAsync(int processId)
        {
            LogContext.PushProperty("ActivityName", "Verify");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostSaveAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);
            var action = "Save";
            LogContext.PushProperty("Action", action);


            _logger.LogInformation("Entering Save with: ProcessId: {ProcessId}; ActivityName: {ActivityName}; Action: {Action};");

            ValidationErrorMessages.Clear();

            var workflowInstance = _dbContext.WorkflowInstance
                .Include(wi => wi.PrimaryDocumentStatus)
                .FirstOrDefault(wi => wi.ProcessId == processId);

            if (workflowInstance == null)
            {
                _logger.LogError("ProcessId {ProcessId} does not appear in the WorkflowInstance table", ProcessId);
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the WorkflowInstance table");
            }

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} has already been completed");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} has already been completed");
                throw appException;
            }

            ProcessId = processId;

            if (!await _pageValidationHelper.CheckVerifyPageForErrors(action,
                Ion,
                ActivityCode,
                SourceCategory,
                Verifier,
                ProductActioned,
                ProductActionChangeDetails,
                RecordProductAction,
                DataImpacts,
                StsDataImpact,
                Team,
                ValidationErrorMessages,
                CurrentUser.DisplayName))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)VerifyCustomHttpStatusCode.FailedValidation
                };
            }

            if (!await SaveTaskData(processId, workflowInstance.WorkflowInstanceId))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)VerifyCustomHttpStatusCode.FailuresDetected
                };
            }

            _logger.LogInformation("Finished Save with: ProcessId: {ProcessId}; Action: {Action};");

            return StatusCode((int)HttpStatusCode.OK);

        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            LogContext.PushProperty("ActivityName", "Verify");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("Action", action);
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            _logger.LogInformation("Entering Done with: ProcessId: {ProcessId}; ActivityName: {ActivityName}; Action: {Action};");

            ValidationErrorMessages.Clear();

            var workflowInstance = _dbContext.WorkflowInstance
                .Include(wi => wi.PrimaryDocumentStatus)
                .FirstOrDefault(wi => wi.ProcessId == processId);

            if (workflowInstance == null)
            {
                _logger.LogError("ProcessId {ProcessId} does not appear in the WorkflowInstance table", ProcessId);
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the WorkflowInstance table");
            }

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} has already been completed");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} has already been completed");
                throw appException;
            }

            ProcessId = processId;
            await GetOnHoldData(processId);

            switch (action)
            {
                case "Done":
                    var verifyData =
                        await _dbContext.DbAssessmentVerifyData.FirstAsync(t =>
                            t.ProcessId == processId);
                    if (!await _pageValidationHelper.CheckVerifyPageForErrors(// from submitted form data
                        action,
                        Ion,
                        ActivityCode,
                        SourceCategory,
                        Verifier,
                        ProductActioned,
                        ProductActionChangeDetails,
                        RecordProductAction,
                        DataImpacts,
                        StsDataImpact,
                        Team,
                        ValidationErrorMessages,
                        CurrentUser.UserPrincipalName,
                        verifyData.Verifier, IsOnHold)) // from database

                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.FailedValidation
                        };
                    }

                    var hasWarnings = await _pageValidationHelper.CheckVerifyPageForWarnings(action, workflowInstance, DataImpacts, StsDataImpact, ValidationErrorMessages);

                    if (!await MarkCarisProjectAsComplete(processId))
                    {
                        hasWarnings = true;
                    }

                    if (hasWarnings)
                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.WarningsDetected
                        };

                    }

                    try
                    {
                        await MarkTaskAsComplete(processId);
                    }
                    catch (Exception e)
                    {
                        await MarkWorkflowInstanceAsStarted(processId);

                        _logger.LogError("Unable to sign-off task {ProcessId}.", e);

                        ValidationErrorMessages.Add($"Unable to sign-off task. Please retry later: {e.Message}");

                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.FailuresDetected
                        };
                    }

                    break;
                case "ConfirmedSignOff":

                    try
                    {
                        await MarkTaskAsComplete(processId);
                    }
                    catch (Exception e)
                    {
                        await MarkWorkflowInstanceAsStarted(processId);

                        _logger.LogError("Unable to sign-off task {ProcessId}.", e);

                        ValidationErrorMessages.Add($"Unable to sign-off task. Please retry later: {e.Message}");

                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.FailuresDetected
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

        public async Task<IActionResult> OnPostRejectVerifyAsync(int processId, string comment)
        {
            LogContext.PushProperty("ActivityName", "Verify");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostRejectVerifyAsync));
            LogContext.PushProperty("Comment", comment);
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            _logger.LogInformation("Entering Reject with: ProcessId: {ProcessId}; Comment: {Comment};");

            ValidationErrorMessages.Clear();

            var verifyData = await _dbContext.DbAssessmentVerifyData.FirstAsync(v => v.ProcessId == processId);

            if (verifyData.Verifier is null)
            {
                ValidationErrorMessages.Add($"Operators: You are not assigned as the Verifier of this task. Please assign the task to yourself and click Save");
            }
            else if (!CurrentUser.UserPrincipalName.Equals(verifyData.Verifier.UserPrincipalName, StringComparison.InvariantCultureIgnoreCase))
            {
                ValidationErrorMessages.Add($"Operators: {verifyData.Verifier.DisplayName} is assigned to this task. Please assign the task to yourself and click Save");
            }

            if (ValidationErrorMessages.Any())
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)VerifyCustomHttpStatusCode.FailedValidation
                };
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                _logger.LogError("Comment is null, empty or whitespace: {Comment}");
                ValidationErrorMessages.Add($"Reject comment cannot be empty.");

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)VerifyCustomHttpStatusCode.FailedValidation
                };
            }

            if (processId < 1)
            {
                _logger.LogError("ProcessId is less than 1: {ProcessId}");
                throw new ArgumentException($"{nameof(processId)} is less than 1");
            }

            var workflowInstance = _dbContext.WorkflowInstance
                .Include(p => p.PrimaryDocumentStatus)
                .FirstOrDefault(wi => wi.ProcessId == processId);

            if (workflowInstance == null)
            {
                _logger.LogError("ProcessId {ProcessId} does not appear in the WorkflowInstance table", ProcessId);
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the WorkflowInstance table");
            }

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} has already been completed");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} has already been completed");
                throw appException;
            }

            _logger.LogInformation("Rejecting: ProcessId: {ProcessId}; Comment: {Comment};");

            try
            {
                await MarkTaskAsRejected(processId, comment);
            }
            catch (Exception e)
            {
                await MarkWorkflowInstanceAsStarted(processId);

                _logger.LogError("Unable to reject task {ProcessId}.", e);

                ValidationErrorMessages.Add($"Unable to reject task. Please retry later: {e.Message}");

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)VerifyCustomHttpStatusCode.FailuresDetected
                };
            }

            return StatusCode((int)HttpStatusCode.OK);
        }

        private async Task<bool> SaveTaskData(int processId, int workflowInstanceId)
        {
            await UpdateOnHold(processId, IsOnHold);
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

            await _dbAssessmentCommentsHelper.AddComment($"Verify: Changes saved",
                processId,
                workflowInstanceId,
                CurrentUser.UserPrincipalName);

            return true;
        }

        private async Task<bool> MarkCarisProjectAsComplete(int processId)
        {
            var carisProjectDetails = await
                _dbContext.CarisProjectDetails.SingleOrDefaultAsync(c => c.ProcessId == processId);

            if (carisProjectDetails != null)
            {
                try
                {
                    await _carisProjectHelper.MarkCarisProjectAsComplete(carisProjectDetails.ProjectId, _generalConfig.Value.CarisProjectTimeoutSeconds);
                }
                catch (Exception e)
                {
                    ValidationErrorMessages.Add($"Caris Project: '{carisProjectDetails.ProjectName}' failed to be marked as Completed: {e.Message}");
                    return false;
                }
            }

            return true;
        }

        private async Task MarkTaskAsComplete(int processId)
        {
            var workflowInstance = await MarkWorkflowInstanceAsUpdating(processId);

            await PublishProgressWorkflowInstanceEvent(processId, workflowInstance, WorkflowStage.Verify, WorkflowStage.Completed);

            _logger.LogInformation(
                "Task sign-off from {ActivityName} has been triggered by {userPrincipcalname} with: ProcessId: {ProcessId}; Action: {Action};");

            await _dbAssessmentCommentsHelper.AddComment("Task sign-off has been triggered",
                processId,
                workflowInstance.WorkflowInstanceId,
                CurrentUser.UserPrincipalName);
        }

        private async Task MarkTaskAsRejected(int processId, string comment)
        {
            var workflowInstance = await MarkWorkflowInstanceAsUpdating(processId);

            await _dbAssessmentCommentsHelper.AddComment($"Verify Rejected: {comment}",
                                                    processId,
                                                    workflowInstance.WorkflowInstanceId,
                                                    CurrentUser.UserPrincipalName);

            await PublishProgressWorkflowInstanceEvent(processId, workflowInstance, WorkflowStage.Verify, WorkflowStage.Rejected);

            _logger.LogInformation(
                "Task rejection from {ActivityName} has been triggered by {UserPrincipalName} with: ProcessId: {ProcessId}; Action: {Action};");

            await _dbAssessmentCommentsHelper.AddComment("Task rejection has been triggered",
                                                    processId,
                                                    workflowInstance.WorkflowInstanceId,
                                                    CurrentUser.UserPrincipalName);
        }

        private async Task PublishProgressWorkflowInstanceEvent(int processId, WorkflowInstance workflowInstance, WorkflowStage fromActivity, WorkflowStage toActivity)
        {
            var correlationId = workflowInstance.PrimaryDocumentStatus.CorrelationId;

            var progressWorkflowInstanceEvent = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = correlationId ?? Guid.NewGuid(),
                ProcessId = processId,
                FromActivity = fromActivity,
                ToActivity = toActivity
            };

            LogContext.PushProperty("ProgressWorkflowInstanceEvent",
                progressWorkflowInstanceEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing ProgressWorkflowInstanceEvent: {ProgressWorkflowInstanceEvent};");
            await _eventServiceApiClient.PostEvent(nameof(ProgressWorkflowInstanceEvent), progressWorkflowInstanceEvent);
            _logger.LogInformation("Published ProgressWorkflowInstanceEvent: {ProgressWorkflowInstanceEvent};");
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
            var onHoldRows = await _dbContext.OnHold.AsNoTracking().Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }

        private async Task UpdateTaskInformation(int processId)
        {
            var currentVerify = await _dbContext.DbAssessmentVerifyData.FirstAsync(r => r.ProcessId == processId);

            currentVerify.Verifier = Verifier;
            currentVerify.Ion = Ion;
            currentVerify.ActivityCode = ActivityCode;
            currentVerify.SourceCategory = SourceCategory;

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
            var currentVerify = await _dbContext.DbAssessmentVerifyData.FirstAsync(r => r.ProcessId == processId);

            currentVerify.ProductActioned = ProductActioned;
            currentVerify.ProductActionChangeDetails = ProductActionChangeDetails;

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
            var currentVerify = await _dbContext.DbAssessmentVerifyData.FirstAsync(r => r.ProcessId == processId);

            var carisProjectDetails = await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);
            var isCarisProjectCreated = carisProjectDetails != null;

            if (isCarisProjectCreated)
            {
                // just update Caris project Assigned users
                await UpdateCarisProjectWithAdditionalUser(processId, currentVerify.Assessor.DisplayName);
                await UpdateCarisProjectWithAdditionalUser(processId, currentVerify.Verifier.DisplayName);

                return;
            }

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

        private async Task<HpdUser> GetHpdUser(string userPrincipalName)
        {
            try
            {
                return await _dbContext.HpdUser.SingleAsync(u => u.AdUser.UserPrincipalName.Equals(userPrincipalName,
                    StringComparison.InvariantCultureIgnoreCase));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserPrincipalName} in our system.");
                throw new InvalidOperationException($"Edit Database: Unable to find HPD username for {userPrincipalName} in our system.",
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

        private async Task<WorkflowInstance> MarkWorkflowInstanceAsUpdating(int processId)
        {
            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(w => w.PrimaryDocumentStatus)
                .FirstAsync(wi => wi.ProcessId == processId);

            workflowInstance.Status = WorkflowStatus.Updating.ToString();
            workflowInstance.ActivityChangedAt = DateTime.Today;
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
    }
}