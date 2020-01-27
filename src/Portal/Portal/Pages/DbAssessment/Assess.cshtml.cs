using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Events;
using HpdDatabase.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Portal.Auth;
using Portal.Helpers;
using Portal.HttpClients;
using Portal.Models;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class AssessModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly HpdDbContext _hpdDbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly ILogger<AssessModel> _logger;
        private readonly ICommentsHelper _commentsHelper;
        private readonly IUserIdentityService _userIdentityService;
        private readonly IRecordProductActionHelper _recordProductActionHelper;

        public bool IsOnHold { get; set; }
        public int ProcessId { get; set; }

        [BindProperty]
        public string Ion { get; set; }

        [BindProperty]
        public string ActivityCode { get; set; }

        [BindProperty]
        public string SourceCategory { get; set; }

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
        public List<DataImpact> DataImpacts { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public AssessModel(WorkflowDbContext dbContext,
            HpdDbContext hpdDbContext,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            ILogger<AssessModel> logger,
            ICommentsHelper commentsHelper,
            IUserIdentityService userIdentityService,
            IRecordProductActionHelper recordProductActionHelper)
        {
            _dbContext = dbContext;
            _hpdDbContext = hpdDbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _logger = logger;
            _commentsHelper = commentsHelper;
            _userIdentityService = userIdentityService;
            _recordProductActionHelper = recordProductActionHelper;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGet(int processId)
        {
            //TODO: Read operators from DB

            ProcessId = processId;
            OperatorsModel = await GetOperatorsData(processId);
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId, [FromQuery] string action)
        {
            LogContext.PushProperty("ActivityName", "Assess");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDoneAsync));
            LogContext.PushProperty("Action", action);

            _logger.LogInformation("Entering Done with: ProcessId: {ProcessId}; ActivityName: {ActivityName}; Action: {Action};");
            
            var isValid = true;
            ValidationErrorMessages.Clear();

            if (!ValidateTaskInformation())
            {
                isValid = false;
            }

            if (!ValidateOperators())
            {
                isValid = false;
            }

            if (!await _recordProductActionHelper.ValidateRecordProductAction(RecordProductAction, ValidationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateDataImpact())
            {
                isValid = false;
            }

            if (!isValid)
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            await UpdateTaskInformation(processId);

            await UpdateProductAction(processId);

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


        private bool ValidateTaskInformation()
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(Ion))
            {
                ValidationErrorMessages.Add("Task Information: Ion cannot be empty");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(ActivityCode))
            {
                ValidationErrorMessages.Add("Task Information: Activity code cannot be empty");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(SourceCategory))
            {
                ValidationErrorMessages.Add("Task Information: Source category cannot be empty");
                isValid = false;
            }
            
            return isValid;
        }

        private bool ValidateOperators()
        {
            if (string.IsNullOrWhiteSpace(Verifier))
            {
                ValidationErrorMessages.Add("Operators: Verifier cannot be empty");
                return false;
            }
            return true;
        }

        private bool ValidateDataImpact()
        {
            // Show error to user, that they've chosen the same usage more than once

            if (DataImpacts.GroupBy(x => x.HpdUsageId)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key).Any())
            {
                ValidationErrorMessages.Add("Data Impact: More than one of the same Usage selected");
                return false;
            }

            return true;
        }

        private async Task UpdateTaskInformation(int processId)
        {
            var currentAssess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);
            currentAssess.Verifier = Verifier;
            currentAssess.Ion = Ion;
            currentAssess.ActivityCode = ActivityCode;
            currentAssess.SourceCategory = SourceCategory;
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

        private async Task UpdateDataImpact(int processId)
        {
            var toRemove = await _dbContext.DataImpact.Where(at => at.ProcessId == processId).ToListAsync();
            _dbContext.DataImpact.RemoveRange(toRemove);

            foreach (var dataImpact in DataImpacts)
            {
                dataImpact.ProcessId = processId;
                _dbContext.DataImpact.Add(dataImpact);
            }

            await _dbContext.SaveChangesAsync();
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

        private async Task<_OperatorsModel> GetOperatorsData(int processId)
        {
            if (!System.IO.File.Exists(@"Data\Users.json")) throw new FileNotFoundException(@"Data\Users.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\Users.json");
            var users = JsonConvert.DeserializeObject<IEnumerable<Assessor>>(jsonString)
                .Select(u => u.Name)
                .ToList();

            var currentAssess = await _dbContext.DbAssessmentAssessData.FirstAsync(r => r.ProcessId == processId);


            return new _OperatorsModel
            {
                Reviewer = currentAssess.Reviewer ?? "Unknown",
                Assessor = currentAssess.Assessor ?? "Unknown",
                Verifier = currentAssess.Verifier ?? "",
                Verifiers = new SelectList(users)
            };
        }

        private async Task GetOnHoldData(int processId)
        {
            var onHoldRows = await _dbContext.OnHold.Where(r => r.ProcessId == processId).ToListAsync();
            IsOnHold = onHoldRows.Any(r => r.OffHoldTime == null);
        }
    }
}