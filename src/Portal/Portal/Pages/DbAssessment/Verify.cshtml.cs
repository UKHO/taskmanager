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
        private readonly ICommentsHelper _commentsHelper;
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILogger<VerifyModel> _logger;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;

        [BindProperty]
        public bool IsOnHold { get; set; }
        public int ProcessId { get; set; }
        public _OperatorsModel OperatorsModel { get; set; }

        [BindProperty]
        public List<ProductAction> RecordProductAction { get; set; }

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public List<string> ValidationErrorMessages { get; set; }

        [BindProperty]
        public string Ion { get; set; }

        [BindProperty]
        public string ActivityCode { get; set; }

        [BindProperty]
        public string SourceCategory { get; set; }

        [BindProperty]
        public string Verifier { get; set; }

        [BindProperty]
        public List<DataImpact> DataImpacts { get; set; }

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
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            ICommentsHelper commentsHelper,
            IUserIdentityService userIdentityService,
            ILogger<VerifyModel> logger,
            IPageValidationHelper pageValidationHelper,
            ICarisProjectHelper carisProjectHelper,
            IOptions<GeneralConfig> generalConfig)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _commentsHelper = commentsHelper;
            _userIdentityService = userIdentityService;
            _logger = logger;
            _pageValidationHelper = pageValidationHelper;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;

            var currentVerifyData = await _dbContext.DbAssessmentVerifyData.FirstAsync(r => r.ProcessId == processId);
            OperatorsModel = _OperatorsModel.GetOperatorsData(currentVerifyData);
            OperatorsModel.ParentPage = WorkflowStage = WorkflowStage.Verify;
            SerialisedCustomHttpStatusCodes = EnumHandlers.EnumToString<VerifyCustomHttpStatusCode>();

            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            LogContext.PushProperty("ActivityName", "Verify");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("Action", action);

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            LogContext.PushProperty("UserFullName", UserFullName);

            _logger.LogInformation("Entering Done with: ProcessId: {ProcessId}; ActivityName: {ActivityName}; Action: {Action};");

            ValidationErrorMessages.Clear();

            ProcessId = processId;

            var workflowInstance = await _dbContext.WorkflowInstance
                .Include(w => w.PrimaryDocumentStatus)
                .FirstAsync(w => w.ProcessId == processId);

            switch (action)
            {
                case "Save":
                    if (!await _pageValidationHelper.CheckVerifyPageForErrors(action,
                        Ion, ActivityCode, SourceCategory, Verifier, RecordProductAction, DataImpacts, Team, ValidationErrorMessages, UserFullName))
                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.FailedValidation
                        };
                    }

                    if (!await SaveTaskData(processId))
                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.FailuresDetected
                        };
                    }

                    return StatusCode((int)HttpStatusCode.OK);
                case "Done":
                    var verifyData =
                        await _dbContext.DbAssessmentVerifyData.FirstAsync(t =>
                            t.ProcessId == processId);
                    if (!await _pageValidationHelper.CheckVerifyPageForErrors(// from submitted form data
                        action,
                                                                        Ion,
                                                                        ActivityCode,
                                                                        SourceCategory, Verifier, RecordProductAction, DataImpacts, Team, ValidationErrorMessages, UserFullName, verifyData.Verifier)) // from database

                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.FailedValidation
                        };
                    }

                    if (!await SaveTaskData(processId))
                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.FailuresDetected
                        };
                    }


                    var hasWarnings = await _pageValidationHelper.CheckVerifyPageForWarnings(action, workflowInstance, DataImpacts, ValidationErrorMessages);

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

                    if (!await MarkTaskAsComplete(processId, workflowInstance))
                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)HttpStatusCode.InternalServerError
                        };
                    }

                    return StatusCode((int)HttpStatusCode.OK);
                case "ConfirmedSignOff":

                    if (!await MarkTaskAsComplete(processId, workflowInstance))
                    {
                        return new JsonResult(this.ValidationErrorMessages)
                        {
                            StatusCode = (int)VerifyCustomHttpStatusCode.FailuresDetected
                        };
                    }

                    return StatusCode((int)HttpStatusCode.OK);
                default:
                    _logger.LogError("Action not found {Action}");

                    throw new NotImplementedException($"Action not found {action}");
            }
        }

        public async Task<IActionResult> OnPostRejectVerifyAsync(int processId, string comment)
        {
            LogContext.PushProperty("ActivityName", "Verify");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostRejectVerifyAsync));
            LogContext.PushProperty("Comment", comment);

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);
            LogContext.PushProperty("UserFullName", UserFullName);

            _logger.LogInformation("Entering Reject with: ProcessId: {ProcessId}; Comment: {Comment};");

            ValidationErrorMessages.Clear();

            var verifyData = await _dbContext.DbAssessmentVerifyData.FirstAsync(v => v.ProcessId == processId);

            if (string.IsNullOrWhiteSpace(verifyData.Verifier))
            {
                ValidationErrorMessages.Add($"Operators: You are not assigned as the Verifier of this task. Please assign the task to yourself and click Save");
            }
            else if (!UserFullName.Equals(verifyData.Verifier, StringComparison.InvariantCultureIgnoreCase))
            {
                ValidationErrorMessages.Add($"Operators: {verifyData.Verifier} is assigned to this task. Please assign the task to yourself and click Save");
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

            _logger.LogInformation("Rejecting: ProcessId: {ProcessId}; Comment: {Comment};");

            // TODO: Update K2 and persist
            var workflowInstance = await _dbContext.WorkflowInstance
                                                        .Include(p => p.PrimaryDocumentStatus)
                                                        .FirstAsync(w => w.ProcessId == processId);
            
            if (!await MarkTaskAsRejected(processId, workflowInstance))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)VerifyCustomHttpStatusCode.FailuresDetected
                };
            }

            await _commentsHelper.AddComment($"Verify Rejected: {comment}",
                processId,
                workflowInstance.WorkflowInstanceId,
                UserFullName);

            return StatusCode((int)HttpStatusCode.OK);
        }

        private async Task<bool> HasActiveChildTasks(WorkflowInstance workflowInstance)
        {
            if (workflowInstance.ParentProcessId == null)
            {
                var childProcessIds = await _dbContext.WorkflowInstance
                    .Where(wi => wi.ParentProcessId == workflowInstance.ProcessId)
                    .Where(wi =>
                        wi.Status == WorkflowStatus.Started.ToString() || wi.Status == WorkflowStatus.Updating.ToString())
                    .Select(wi => wi.ProcessId)
                    .ToListAsync();


                if (childProcessIds.Any())
                {
                    var joined = string.Join(',', childProcessIds);

                    ValidationErrorMessages.Add($"Child Tasks: The current task has the following active child tasks: {joined}.");

                    return true;
                }
            }

            return false;
        }

        private async Task<bool> SaveTaskData(int processId)
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

        private async Task<bool> MarkTaskAsComplete(int processId, WorkflowInstance workflowInstance)
        {
            workflowInstance.Status = WorkflowStatus.Updating.ToString();

            await _dbContext.SaveChangesAsync();

            var success = await _workflowServiceApiClient.ProgressWorkflowInstance(workflowInstance.ProcessId,
                workflowInstance.SerialNumber, "Verify", "Complete");

            if (success)
            {
                await PersistCompletedVerify(processId, workflowInstance);

                _logger.LogInformation(
                    "{UserFullName} successfully progressed {ActivityName} to Completed on 'Done' button with: ProcessId: {ProcessId}; Action: {Action};");

                await _commentsHelper.AddComment($"Verify step completed",
                    processId,
                    workflowInstance.WorkflowInstanceId,
                    UserFullName);
            }
            else
            {
                workflowInstance.Status = WorkflowStatus.Started.ToString();
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Unable to progress task {ProcessId} from Verify to Completed.");

                ValidationErrorMessages.Add("Unable to progress task from Verify to Completed. Please retry later.");

                return false;
            }


            _logger.LogInformation("Finished Done with: ProcessId: {ProcessId}; Action: {Action};");

            return true;
        }

        private async Task<bool> MarkTaskAsRejected(int processId, WorkflowInstance workflowInstance)
        {
            workflowInstance.Status = WorkflowStatus.Updating.ToString();

            await _dbContext.SaveChangesAsync();

            var success = await _workflowServiceApiClient.RejectWorkflowInstance(workflowInstance.ProcessId,
                workflowInstance.SerialNumber, "Verify", "Assess");

            if (success)
            {
                _logger.LogInformation("{UserFullName} successfully rejected task with: ProcessId: {ProcessId}; Comment: {Comment};");

                await PersistRejectedVerify(processId, workflowInstance);

                _logger.LogInformation("Finished Reject with: ProcessId: {ProcessId}; Action: {Action};");

                return true;

            }

            workflowInstance.Status = WorkflowStatus.Started.ToString();
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Unable to reject task {ProcessId} from Verify to Assess.");

            ValidationErrorMessages.Add("Unable to reject task from Assess to Verify. Please retry later.");

            return false;
        }

        private async Task PersistCompletedVerify(int processId, WorkflowInstance workflowInstance)
        {
            var correlationId = workflowInstance.PrimaryDocumentStatus.CorrelationId;

            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = correlationId.HasValue ? correlationId.Value : Guid.NewGuid(),
                ProcessId = processId,
                FromActivity = WorkflowStage.Verify,
                ToActivity = WorkflowStage.Completed
            };

            LogContext.PushProperty("PersistWorkflowInstanceDataEvent",
                persistWorkflowInstanceDataEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
            await _eventServiceApiClient.PostEvent(nameof(PersistWorkflowInstanceDataEvent), persistWorkflowInstanceDataEvent);
            _logger.LogInformation("Published PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
        }

        private async Task PersistRejectedVerify(int processId, WorkflowInstance workflowInstance)
        {
            var correlationId = workflowInstance.PrimaryDocumentStatus.CorrelationId;

            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = correlationId ?? Guid.NewGuid(),
                ProcessId = processId,
                FromActivity = WorkflowStage.Verify,
                ToActivity = WorkflowStage.Assess
            };

            LogContext.PushProperty("PersistWorkflowInstanceDataEvent",
                persistWorkflowInstanceDataEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
            await _eventServiceApiClient.PostEvent(nameof(PersistWorkflowInstanceDataEvent), persistWorkflowInstanceDataEvent);
            _logger.LogInformation("Published PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
        }

        private async Task UpdateOnHold(int processId, bool onHold)
        {
            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

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

        private async Task UpdateSdraAssessmentAsCompleted(string comment, WorkflowInstance workflowInstance)
        {
            // TODO: Mark SDRA Assessment as completed
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
                await UpdateCarisProjectWithAdditionalUser(processId, currentVerify.Assessor);
                await UpdateCarisProjectWithAdditionalUser(processId, currentVerify.Verifier);

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
    }
}