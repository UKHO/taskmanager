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
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly ILogger<AssessModel> _logger;
        private readonly ICommentsHelper _commentsHelper;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;

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

        public string SerialisedCustomHttpStatusCodes { get; set; }

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
            IAdDirectoryService adDirectoryService,
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
            _adDirectoryService = adDirectoryService;
            _pageValidationHelper = pageValidationHelper;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;

            var currentAssessData = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);
            OperatorsModel = _OperatorsModel.GetOperatorsData(currentAssessData);
            OperatorsModel.ParentPage = WorkflowStage = WorkflowStage.Assess;
            SerialisedCustomHttpStatusCodes = EnumHandlers.EnumToString<AssessCustomHttpStatusCode>();

            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            LogContext.PushProperty("ActivityName", "Assess");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("Action", action);

            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

            LogContext.PushProperty("UserFullName", UserFullName);

            _logger.LogInformation("Entering Done with: ProcessId: {ProcessId}; ActivityName: {ActivityName}; Action: {Action};");

            ValidationErrorMessages.Clear();

            ProcessId = processId;

            var currentAssessData = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);

            switch (action)
            {
                case "Save":
                    if (!await _pageValidationHelper.CheckAssessPageForErrors(
                        action,
                        Ion,
                        ActivityCode,
                        SourceCategory,
                        TaskType,
                        ProductActioned,
                        ProductActionChangeDetails,
                        RecordProductAction,
                        DataImpacts, Team, Assessor, Verifier, ValidationErrorMessages, UserFullName, currentAssessData.Assessor))
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

                    break;
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
                        DataImpacts, Team, Assessor, Verifier, ValidationErrorMessages, UserFullName, currentAssessData.Assessor))
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

                    var hasWarnings = _pageValidationHelper.CheckAssessPageForWarnings(action, DataImpacts, ValidationErrorMessages);

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
                "{UserFullName} successfully progressed {ActivityName} to Verify on 'Done' button with: ProcessId: {ProcessId}; Action: {Action};");
            
            await _commentsHelper.AddComment($"Assess step completed",
                                                                        processId,
                                                                    workflowInstance.WorkflowInstanceId,
                                                                    UserFullName);
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

            await _commentsHelper.AddComment($"Assess: Changes saved",
                processId,
                workflowInstanceId,
                UserFullName);


            return true;
        }

        private async Task<string> GetCurrentAssessor(int processId)
        {
            var currentAssessData = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);
            return currentAssessData.Assessor;
        }

        private async Task UpdateTaskInformation(int processId)
        {
            var currentAssess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);

            currentAssess.Assessor = Assessor;
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

        private async Task UpdateOnHold(int processId, bool onHold)
        {
            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

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
                    OnHoldUser = UserFullName,
                    WorkflowInstanceId = workflowInstance.WorkflowInstanceId
                };

                await _dbContext.OnHold.AddAsync(onHoldRecord);
                await _dbContext.SaveChangesAsync();

                await _commentsHelper.AddComment($"Task {processId} has been put on hold",
                    processId,
                    workflowInstance.WorkflowInstanceId,
                    UserFullName);
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
                existingOnHoldRecord.OffHoldUser = UserFullName;

                await _dbContext.SaveChangesAsync();

                await _commentsHelper.AddComment($"Task {processId} taken off hold",
                    processId,
                    _dbContext.WorkflowInstance.First(p => p.ProcessId == processId)
                        .WorkflowInstanceId,
                    UserFullName);
            }
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}