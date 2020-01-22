using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
    public class VerifyModel : PageModel
    {
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly ICommentsHelper _commentsHelper;
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILogger<VerifyModel> _logger;
        private readonly WorkflowDbContext _dbContext;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IWorkflowServiceApiClient _workflowServiceApiClient;
        private readonly IEventServiceApiClient _eventServiceApiClient;

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

        public VerifyModel(WorkflowDbContext dbContext,
            IDataServiceApiClient dataServiceApiClient,
            IWorkflowServiceApiClient workflowServiceApiClient,
            IEventServiceApiClient eventServiceApiClient,
            IOptions<UriConfig> uriConfig,
            ICommentsHelper commentsHelper,
            IUserIdentityService userIdentityService,
            ILogger<VerifyModel> logger)
        {
            _dbContext = dbContext;
            _dataServiceApiClient = dataServiceApiClient;
            _workflowServiceApiClient = workflowServiceApiClient;
            _eventServiceApiClient = eventServiceApiClient;
            _uriConfig = uriConfig;
            _commentsHelper = commentsHelper;
            _userIdentityService = userIdentityService;
            _logger = logger;
        }

        public async Task OnGet(int processId)
        {
            ProcessId = processId;
            OperatorsModel = SetOperatorsDummyData();
            await GetOnHoldData(processId);
        }

        public async Task<IActionResult> OnPostDoneAsync(int processId)
        {
            return RedirectToPage("/Index");
        }


        public async Task<IActionResult> OnPostRejectVerifyAsync(string comment, int processId)
        {
            LogContext.PushProperty("ActivityName", "Verify");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnPostRejectVerifyAsync));
            LogContext.PushProperty("Comment", comment);

            _logger.LogInformation("Entering Reject with: ProcessId: {ProcessId}; Comment: {Comment};");

            if (string.IsNullOrWhiteSpace(comment))
            {
                _logger.LogError("Comment is null, empty or whitespace: {Comment}");
                throw new ArgumentException($"{nameof(comment)} is null, empty or whitespace");
            }

            if (processId < 1)
            {
                _logger.LogError("ProcessId is less than 1: {ProcessId}");
                throw new ArgumentException($"{nameof(processId)} is less than 1");
            }

            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

            LogContext.PushProperty("UserFullName", UserFullName);

            _logger.LogInformation("Rejecting: ProcessId: {ProcessId}; Comment: {Comment};");

            // TODO: Update K2 and persist
            var workflowInstance = await _dbContext.WorkflowInstance
                                                        .Include(p => p.PrimaryDocumentStatus)
                                                        .FirstAsync(w => w.ProcessId == processId);

            await _commentsHelper.AddComment($"Reject comment: {comment}",
                processId,
                workflowInstance.WorkflowInstanceId,
                UserFullName);

            workflowInstance.Status = WorkflowStatus.Updating.ToString();

            await _dbContext.SaveChangesAsync();


            var success = await _workflowServiceApiClient.RejectWorkflowInstance(workflowInstance.ProcessId, workflowInstance.SerialNumber, "Verify", "Assess");

            if (success)
            {
                await PersistRejectedVerify(processId, workflowInstance);
            }

            _logger.LogInformation("Rejected successfully with: ProcessId: {ProcessId}; Comment: {Comment};");

            return RedirectToPage("/Index");
        }

        private async Task PersistRejectedVerify(int processId, WorkflowInstance workflowInstance)
        {
            var correlationId = workflowInstance.PrimaryDocumentStatus.CorrelationId;

            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = correlationId ?? Guid.NewGuid(),
                ProcessId = processId,
                FromActivityName = "Verify",
                ToActivityName = "Assess"
            };

            LogContext.PushProperty("PersistWorkflowInstanceDataEvent",
                persistWorkflowInstanceDataEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
            await _eventServiceApiClient.PostEvent(nameof(PersistWorkflowInstanceDataEvent), persistWorkflowInstanceDataEvent);
            _logger.LogInformation("Published PersistWorkflowInstanceDataEvent: {PersistWorkflowInstanceDataEvent};");
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

        private _OperatorsModel SetOperatorsDummyData()
        {
            if (!System.IO.File.Exists(@"Data\Users.json")) throw new FileNotFoundException(@"Data\Users.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\Users.json");
            var users = JsonConvert.DeserializeObject<IEnumerable<Assessor>>(jsonString)
                .Select(u => u.Name)
                .ToList();

            return new _OperatorsModel
            {
                Reviewer = "Greg Williams",
                Assessor = "Peter Bates",
                Verifier = "Matt Stoodley",
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