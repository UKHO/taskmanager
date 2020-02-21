using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
using NCNEPortal.Enums;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TaskComment = NCNEPortal.Models.TaskComment;

namespace NCNEPortal
{
    public class WorkflowModel : PageModel
    {
        private readonly NcneWorkflowDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly IUserIdentityService _userIdentityService;
        private readonly ICommentsHelper _commentsHelper;
        public int ProcessId { get; set; }

        [DisplayName("ION")] public string Ion { get; set; }

        [DisplayName("Chart title")] public string ChartTitle { get; set; }

        [DisplayName("Chart number")] public string ChartNo { get; set; }

        [DisplayName("Country")] public string Country { get; set; }

        [DisplayName("Chart type")] public string ChartType { get; set; }

        public SelectList ChartTypes { get; set; }

        [DisplayName("Workflow type")] public string WorkflowType { get; set; }

        public SelectList WorkflowTypes { get; set; }

        [DisplayName("Duration")] public string Dating { get; set; }

        public SelectList DatingList { get; set; }

        [DisplayName("Publication date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime PublicationDate { get; set; }

        [DisplayName("H Forms")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime AnnounceDate { get; set; }

        [DisplayName("Commit to print:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime CommitToPrintDate { get; set; }

        [DisplayName("CIS")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime CISDate { get; set; }

        [DisplayName("Compiler")]
        public string Compiler { get; set; }

        public SelectList CompilerList { get; set; }

        [DisplayName("Verifier V1")]
        public string Verifier1 { get; set; }

        public SelectList VerifierList1 { get; set; }

        [DisplayName("Verifier V2")]
        public string Verifier2 { get; set; }

        public SelectList VerifierList2 { get; set; }

        [DisplayName("Publication")]
        public string Publisher { get; set; }

        public SelectList PublisherList { get; set; }

        [DisplayName("Sent to 3PS")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime SendDate3ps { get; set; }

        [DisplayName("Expected return 3PS")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime ExpectedReturnDate3ps { get; set; }

        [DisplayName("Actual return 3PS")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime ActualReturnDate3ps { get; set; }

        public List<TaskComment> TaskComments { get; set; }

        [DisplayName("CARIS Workspace")]
        public string CarisWorkspace { get; set; }
        public SelectList CarisWorkspaces { get; set; }

        [DisplayName("CARIS Project Name")]
        public string CarisProjectName { get; set; }


        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public WorkflowModel(NcneWorkflowDbContext dbContext,
            ILogger<WorkflowModel> logger,
            IUserIdentityService userIdentityService,
            ICommentsHelper commentsHelper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _userIdentityService = userIdentityService;
            _commentsHelper = commentsHelper;
        }

        public async Task<IActionResult> OnPostTaskTerminateAsync(string comment, int processId)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("NcnePortalResource", nameof(OnPostTaskTerminateAsync));
            LogContext.PushProperty("Comment", comment);

            _logger.LogInformation("Entering terminate with: ProcessId: {ProcessId}; Comment: {Comment};");

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

            _logger.LogInformation("Terminating with: ProcessId: {ProcessId}; Comment: {Comment};");

            var taskInfo = UpdateTaskAsTerminated(processId);

            await _commentsHelper.AddTaskComment($"Terminate comment: {comment}", taskInfo.ProcessId, UserFullName);

            _logger.LogInformation("Terminated successfully with: ProcessId: {ProcessId}; Comment: {Comment};");

            return RedirectToPage("/Index");

        }

        private TaskInfo UpdateTaskAsTerminated(int processId)
        {
            var taskInfo = _dbContext.TaskInfo.FirstOrDefault(t => t.ProcessId == processId);

            if (taskInfo == null)
            {
                _logger.LogError("ProcessId {ProcessId} does not appear in the TaskInfo table", ProcessId);
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the TaskInfo table");
            }

            taskInfo.Status = NcneTaskStatus.Terminated.ToString();
            _dbContext.SaveChanges();

            return taskInfo;
        }

        public void OnGet(int processId)
        {
            ProcessId = processId;
            Ion = "DC782783923;";
            ChartTitle = "Hamoaze";
            ChartNo = "1902";
            WorkflowType = "Standard";
            ChartType = "CME";
            Country = "United Kingdom";
            Dating = "2 Weeks";
            PublicationDate = DateTime.Now.AddDays(30);
            AnnounceDate = DateTime.Now.AddDays(10);
            CommitToPrintDate = DateTime.Now.AddDays(15);
            CISDate = DateTime.Now.AddDays(20);

            Compiler = "BatesP";
            Verifier1 = "StoodleyM";
            Verifier2 = "WillisA";
            Publisher = "AlexanderD";

            SendDate3ps = DateTime.Now.AddDays(-10);
            ExpectedReturnDate3ps = DateTime.Now.AddDays(-2);
            ActualReturnDate3ps = DateTime.Now.AddDays(-1);

            CarisWorkspaces = new SelectList(new List<string>
            {
                "Henballand",
                "Sossalrandfordshire",
                "Wregilliamsville"
            });

            TaskComments = new List<TaskComment>
            {
                new TaskComment()
                {
                    CommentDate = DateTime.Now, Name = "StoodleyM",
                    CommentText = "lsldjkojk sjjksdf kksdfsdf klsdfsdf  sdflks;lksdf  sd;flkkeok;lk';df ", Role = ""
                },
                new TaskComment()
                {
                    CommentDate = DateTime.Now, Name = "WilliamsG",
                    CommentText = "This is a very important comment that will eventually reach a, not insignificant character count. " +
                                  "This is a very important comment that will eventually reach a, not insignificant character count. " +
                                  "This is purely to demonstrate that this will look lovely, nothing more to it than that.",Role = "Verifier 2"
                },
                new TaskComment()
                {
                    CommentDate = DateTime.Now, Name = "StoodleyM",
                    CommentText = "",Role = "Verifier 2"
                },
                new TaskComment()
                {
                    CommentDate = DateTime.Now, Name = "StoodleyM",
                    CommentText = "Rework required as it isn't right.",Role = "Verifier 2"
                }
            };
        }
    }


}
