using Common.Helpers;
using Common.Helpers.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using NCNEPortal.Helpers;
using NCNEPortal.Models;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TaskComment = NCNEWorkflowDatabase.EF.Models.TaskComment;


namespace NCNEPortal
{
    [TypeFilter(typeof(JavascriptError))]
    public class WorkflowModel : PageModel
    {
        private readonly NcneWorkflowDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly ICommentsHelper _commentsHelper;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IMilestoneCalculator _milestoneCalculator;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly INcneUserDbService _ncneUserDbService;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly IWorkflowStageHelper _workflowStageHelper;
        public int ProcessId { get; set; }

        [DisplayName("ION")] [BindProperty] public string Ion { get; set; }

        [DisplayName("Chart title")] public string ChartTitle { get; set; }

        [DisplayName("Chart number")]
        [BindProperty]
        public string ChartNo { get; set; }


        [DisplayName("Country")]
        [BindProperty]
        public string Country { get; set; }

        [DisplayName("Chart type")] public string ChartType { get; set; }

        [DisplayName("Workflow type")] public string WorkflowType { get; set; }

        [BindProperty]
        [DisplayName("Duration")]
        public int Dating { get; set; }


        [DisplayName("Repromat date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        [BindProperty]
        public DateTime? RepromatDate { get; set; }

        [DisplayName("Publication date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        [BindProperty]
        public DateTime? PublicationDate { get; set; }

        [DisplayName("H Forms")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        [BindProperty]
        public DateTime? AnnounceDate { get; set; }

        [DisplayName("Commit to print:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        [BindProperty]
        public DateTime? CommitToPrintDate { get; set; }


        [DisplayName("CIS")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        [BindProperty]
        public DateTime? CISDate { get; set; }

        [DisplayName("Compiler")]
        [BindProperty]
        public string Compiler { get; set; }

        public SelectList CompilerList { get; set; }

        [DisplayName("Verifier V1")]
        [BindProperty]
        public string Verifier1 { get; set; }


        [DisplayName("Verifier V2")]
        [BindProperty]
        public string Verifier2 { get; set; }



        [DisplayName("Publication")]
        [BindProperty]
        public string Publisher { get; set; }

        [BindProperty] public bool SentTo3Ps { get; set; }

        [DisplayName("Sent to 3PS")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        [BindProperty]
        public DateTime? SendDate3ps { get; set; }

        [DisplayName("Expected return 3PS")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        [BindProperty]
        public DateTime? ExpectedReturnDate3ps { get; set; }

        [DisplayName("Actual return 3PS")]
        [BindProperty]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? ActualReturnDate3ps { get; set; }

        public List<TaskComment> TaskComments { get; set; }


        [DisplayName("CARIS Project Name")] public string CarisProjectName { get; set; }

        public string Header { get; set; }

        public CarisProjectDetails CarisProjectDetails { get; set; }
        public bool IsCarisProjectCreated { get; set; }

        public bool CompleteEnabled { get; set; }

        public List<string> ValidationErrorMessages { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;
        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetailsAsync(this.User).Result;
                return _currentUser;
            }
        }

        [BindProperty]
        public List<TaskStage> TaskStages { get; set; }

        public WorkflowModel(NcneWorkflowDbContext dbContext,
            ILogger<WorkflowModel> logger,
            ICommentsHelper commentsHelper,
            ICarisProjectHelper carisProjectHelper,
            IOptions<GeneralConfig> generalConfig,
            IMilestoneCalculator milestoneCalculator,
            IPageValidationHelper pageValidationHelper,
            INcneUserDbService ncneUserDbService,
            IAdDirectoryService adDirectoryService,
            IWorkflowStageHelper workflowStageHelper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _commentsHelper = commentsHelper;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;
            _milestoneCalculator = milestoneCalculator;
            _pageValidationHelper = pageValidationHelper;
            _ncneUserDbService = ncneUserDbService;
            _adDirectoryService = adDirectoryService;
            _workflowStageHelper = workflowStageHelper;

            ValidationErrorMessages = new List<string>();
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

            LogContext.PushProperty("CurrentUser.DisplayName", CurrentUser.DisplayName);

            _logger.LogInformation("Terminating with: ProcessId: {ProcessId}; Comment: {Comment};");

            var taskInfo = UpdateTaskAsTerminated(processId);

            await _commentsHelper.AddTaskComment($"Terminate comment: {comment}", taskInfo.ProcessId, CurrentUser.DisplayName);

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

        public void OnGetAsync(int processId)
        {
            ProcessId = processId;

            var taskInfo = _dbContext.TaskInfo
                .Include(task => task.TaskRole)
                .Include(task => task.TaskStage).ThenInclude(comment => comment.TaskStageComment)
                .Include(task => task.TaskStage).ThenInclude(stagetype => stagetype.TaskStageType)
                .Include(task => task.TaskComment)
                .FirstOrDefault(t => t.ProcessId == processId);


            Ion = taskInfo?.Ion;
            ChartTitle = taskInfo?.ChartTitle;
            ChartNo = taskInfo?.ChartNumber;
            WorkflowType = taskInfo?.WorkflowType;
            ChartType = taskInfo?.ChartType;
            Country = taskInfo?.Country;

            if (taskInfo.Duration == null)
                Dating = 0;
            else
                Dating = (int)Enum.Parse(typeof(DeadlineEnum), taskInfo.Duration);

            RepromatDate = taskInfo.RepromatDate;
            PublicationDate = taskInfo.PublicationDate;
            AnnounceDate = taskInfo.AnnounceDate;
            CommitToPrintDate = taskInfo.CommitDate;
            CISDate = taskInfo.CisDate;

            SentTo3Ps = taskInfo.ThreePs;

            SendDate3ps = taskInfo.SentDate3Ps;
            ExpectedReturnDate3ps = taskInfo.ExpectedDate3Ps;
            ActualReturnDate3ps = taskInfo.ActualDate3Ps;



            Compiler = taskInfo.TaskRole.Compiler;
            Verifier1 = taskInfo.TaskRole.VerifierOne;
            Verifier2 = taskInfo.TaskRole.VerifierTwo;
            Publisher = taskInfo.TaskRole.Publisher;

            TaskStages = taskInfo.TaskStage;


            var carisProject = _dbContext.CarisProjectDetails.FirstOrDefault(c => c.ProcessId == processId);

            TaskComments = taskInfo.TaskComment;

            if (carisProject != null)
            {
                CarisProjectName = carisProject.ProjectName;
                IsCarisProjectCreated = true;
            }
            else if (taskInfo != null) CarisProjectName = $"{ProcessId}_{taskInfo.ChartType}_{taskInfo.ChartNumber}";

            Header = $"{taskInfo.WorkflowType}{(String.IsNullOrEmpty(taskInfo.ChartNumber) ? "" : $" - {taskInfo.ChartNumber}")}";

            var inProgress = TaskStages.FindAll(t => t.Status == NcneTaskStageStatus.InProgress.ToString()
                                                     && t.TaskStageTypeId != (int)NcneTaskStageType.Forms)
                .OrderBy(t => t.TaskStageTypeId);


            Header += " - " + (inProgress.Any() ? inProgress.First().TaskStageType.Name : "Awaiting Completion");


            //Enable complete if Forms and Publication stages are completed.
            CompleteEnabled = TaskStages.Exists(t => t.TaskStageTypeId == (int)NcneTaskStageType.Forms &&
                                                   t.Status == NcneTaskStageStatus.Completed.ToString()) &&
                               TaskStages.Exists(t => t.TaskStageTypeId == (int)NcneTaskStageType.Publication &&
                                                      t.Status == NcneTaskStageStatus.Completed.ToString());



        }




        public async Task<IActionResult> OnPostCreateCarisProjectAsync(int processId, string projectName)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("NcnePortalResource", nameof(OnPostCreateCarisProjectAsync));
            LogContext.PushProperty("ProjectName", projectName);

            _logger.LogInformation("Entering {PortalResource} for Workflow with: ProcessId: {ProcessId}");


            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Please provide a Caris Project Name.");
            }

            LogContext.PushProperty("CurrentUser.DisplayName", CurrentUser.DisplayName);

            var projectId = await CreateCarisProject(processId, projectName);

            await UpdateCarisProjectDetails(processId, projectName, projectId);

            return StatusCode(200);
        }



        private async Task<int> CreateCarisProject(int processId, string projectName)
        {

            var carisProjectDetails =
                await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);

            if (carisProjectDetails != null)
            {
                return carisProjectDetails.ProjectId;
            }

            // which will also implicitly validate if the current user has been mapped to HPD account in our database
            var hpdUser = await GetHpdUser(CurrentUser.DisplayName);

            _logger.LogInformation(
                "Creating Caris Project with ProcessId: {ProcessId}; ProjectName: {ProjectName}.");

            var projectId = await _carisProjectHelper.CreateCarisProject(processId, projectName,
                hpdUser.HpdUsername, _generalConfig.Value.CarisNcneProjectType,
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
                CreatedBy = CurrentUser.DisplayName,
                ProjectId = projectId,
                ProjectName = projectName
            });

            await _dbContext.SaveChangesAsync();
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
                _logger.LogError("Unable to find HPD Username for {CurrentUser.DisplayName} in our system.");
                throw new InvalidOperationException($"Unable to find HPD username for {username}  in our system.",
                    ex.InnerException);
            }

        }


        public async Task<JsonResult> OnPostCalcMilestonesAsync(int deadLine, DateTime dtInput, Boolean isPublish)
        {
            string[] result;
            if (isPublish)
            {
                var dates = _milestoneCalculator.CalculateMilestones((DeadlineEnum)deadLine, (DateTime)dtInput);
                result = new[]
                {
                    dates.formsDate.ToShortDateString(),
                    dates.commitDate.ToShortDateString(),
                    dates.cisDate.ToShortDateString()
                };
            }
            else
            {

                PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)dtInput);

                result = new[]
                {
                    PublicationDate?.ToShortDateString()
                };
            }

            return new JsonResult(result);

        }

        public async Task<IActionResult> OnPostValidateCompleteAsync(int processId, int stageId, string username, int stageTypeId)
        {
            ValidationErrorMessages.Clear();

            try
            {

                var roles = _dbContext.TaskRole.Single(r => r.ProcessId == processId);

                if (!(_pageValidationHelper.ValidateForCompletion(username, CurrentUser.DisplayName, (NcneTaskStageType)stageTypeId, roles, ValidationErrorMessages)))
                {
                    return new JsonResult(this.ValidationErrorMessages)
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }

                return new JsonResult(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<IActionResult> OnPostValidateReworkAsync(int processId, int stageId, string username, int stageTypeId)
        {
            ValidationErrorMessages.Clear();

            if (!(_pageValidationHelper.ValidateForRework(username, CurrentUser.DisplayName, ValidationErrorMessages)))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            return new JsonResult(HttpStatusCode.OK);
        }

        public async Task<IActionResult> OnPostValidateCompleteWorkflow(int processId, string username)
        {
            ValidationErrorMessages.Clear();

            if (!(_pageValidationHelper.ValidateForCompleteWorkflow(username, CurrentUser.DisplayName, ValidationErrorMessages)))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
            return new JsonResult(HttpStatusCode.OK);
        }

        public async Task<IActionResult> OnPostCompleteWorkflow(int processId, string username)
        {
            var taskInfo = _dbContext.TaskInfo.FirstOrDefaultAsync(t => t.ProcessId == processId).Result;

            taskInfo.Status = NcneTaskStatus.Completed.ToString();

            await _commentsHelper.AddTaskSystemComment(NcneCommentType.CompleteWorkflow, processId, CurrentUser.DisplayName, null, null, null);

            await _dbContext.SaveChangesAsync();

            return new JsonResult(HttpStatusCode.OK);

        }

        public async Task<IActionResult> OnPostCompleteAsync(int processId, int stageId, string username,
            Boolean isRework)
        {
            if (isRework)
            {
                await SendtoRework(processId, stageId);
            }
            else
            {
                await CompleteStage(processId, stageId);
            }

            return new JsonResult(HttpStatusCode.OK);
        }

        private async Task<bool> SendtoRework(int processId, int stageId)
        {
            var taskStages = _dbContext.TaskStage.Where(s => s.ProcessId == processId);

            var currentStage = await taskStages.SingleAsync(t => t.TaskStageId == stageId);

            currentStage.Status = NcneTaskStageStatus.Rework.ToString();
            currentStage.DateCompleted = DateTime.Now;
            currentStage.AssignedUser = CurrentUser.DisplayName;

            var nextStage =
                _workflowStageHelper.GetNextStageForRework((NcneTaskStageType)currentStage.TaskStageTypeId);

            taskStages.First(t => t.TaskStageTypeId == (int)nextStage).Status =
                NcneTaskStageStatus.InProgress.ToString();

            var nextStageUser = taskStages.FirstOrDefault(t => t.TaskStageTypeId == (int)nextStage)?
                .AssignedUser;

            var taskInfo = _dbContext.TaskInfo.Single(t => t.ProcessId == processId);

            taskInfo.AssignedUser = nextStageUser;
            taskInfo.AssignedDate = DateTime.Now;

            var stageName = _dbContext.TaskStageType.Single(t => t.TaskStageTypeId == currentStage.TaskStageTypeId).Name;

            await _commentsHelper.AddTaskSystemComment(NcneCommentType.ReworkStage, processId, CurrentUser.DisplayName, stageName, null, null);

            await _dbContext.SaveChangesAsync();


            return true;
        }

        private async Task<bool> CompleteStage(int processId, int stageId)
        {


            var taskStages = _dbContext.TaskStage.Where(s => s.ProcessId == processId);

            var currentStage = await taskStages.SingleAsync(t => t.TaskStageId == stageId);

            currentStage.Status = NcneTaskStageStatus.Completed.ToString();
            currentStage.DateCompleted = DateTime.Now;
            currentStage.AssignedUser = CurrentUser.DisplayName;

            var v2 = taskStages.Single(t => t.ProcessId == processId &&
                                            t.TaskStageTypeId == (int)NcneTaskStageType.V2);

            bool v2Available = (v2.Status != NcneTaskStageStatus.Inactive.ToString());


            var nextStages = _workflowStageHelper.GetNextStagesForCompletion((NcneTaskStageType)currentStage.TaskStageTypeId, v2Available);

            if (nextStages.Count > 0)
            {
                foreach (var stage in nextStages)
                {
                    taskStages.First(t => t.TaskStageTypeId == (int)stage).Status =
                        NcneTaskStageStatus.InProgress.ToString();
                }

                var nextStageUser = taskStages.FirstOrDefault(t => t.TaskStageTypeId == (int)nextStages[0])?
                    .AssignedUser;

                var taskInfo = _dbContext.TaskInfo.Single(t => t.ProcessId == processId);

                taskInfo.AssignedUser = nextStageUser;
                taskInfo.AssignedDate = DateTime.Now;
            }

            var publishInProgress = taskStages.Count(t => t.TaskStageTypeId > (int)NcneTaskStageType.Publication
                                                            && t.TaskStageTypeId != currentStage.TaskStageTypeId
                                                            && t.Status != NcneTaskStageStatus.Completed.ToString());

            var publishStage = taskStages.Single(t => t.TaskStageTypeId == (int)NcneTaskStageType.Publication);

            if (publishInProgress == 0 && publishStage.Status == NcneTaskStageStatus.InProgress.ToString())
            {
                //complete the publication stage
                publishStage.Status = NcneTaskStageStatus.Completed.ToString();
                publishStage.DateCompleted = DateTime.Now;
                publishStage.AssignedUser = CurrentUser.DisplayName;
            }

            var stageName = _dbContext.TaskStageType.Single(t => t.TaskStageTypeId == currentStage.TaskStageTypeId).Name;

            await _commentsHelper.AddTaskSystemComment(currentStage.TaskStageTypeId == (int)NcneTaskStageType.Publish_Chart ? NcneCommentType.CarisPublish : NcneCommentType.CompleteStage,
                processId, CurrentUser.DisplayName, stageName, null, null);

            await _dbContext.SaveChangesAsync();

            return true;
        }


        public async Task<IActionResult> OnPostSaveAsync(int processId, string chartType)
        {

            ValidationErrorMessages.Clear();

            //ValidateUsers(Compiler, Verifier1, Verifier2, Publisher);
            var role = new TaskRole()
            {
                Compiler = Compiler,
                ProcessId = processId,
                VerifierOne = Verifier1,
                VerifierTwo = Verifier2,
                Publisher = Publisher
            };
            var ThreePSInfo = (SentTo3Ps, SendDate3ps, ExpectedReturnDate3ps, ActualReturnDate3ps);


            if (!(_pageValidationHelper.ValidateWorkflowPage(role, PublicationDate, RepromatDate, Dating, chartType,
                ThreePSInfo,
                ValidationErrorMessages)))
            {

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            var result = await UpdateTaskInformation(processId, chartType);


            return new JsonResult(JsonConvert.SerializeObject(result));
        }

        private async Task<DeadlineId> UpdateTaskInformation(int processId, string chartType)
        {
            var task =
                await _dbContext.TaskInfo.Include(t => t.TaskRole)
                      .Include(s => s.TaskStage).FirstAsync(t => t.ProcessId == processId);
            task.Ion = Ion;
            task.ChartNumber = ChartNo;
            task.Duration = Enum.GetName(typeof(DeadlineEnum), Dating);

            if (chartType == "Adoption" && RepromatDate != null)
            {

                PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)RepromatDate);

            }

            await AddSystemComments(task, processId, Compiler, Verifier1, Verifier2, Publisher, CurrentUser.DisplayName);

            task.RepromatDate = RepromatDate;

            task.PublicationDate = PublicationDate;
            task.AnnounceDate = AnnounceDate;
            task.CommitDate = CommitToPrintDate;
            task.CisDate = CISDate;
            task.Country = Country;
            task.ThreePs = SentTo3Ps;
            task.SentDate3Ps = SendDate3ps;
            task.ExpectedDate3Ps = ExpectedReturnDate3ps;
            task.ActualDate3Ps = ActualReturnDate3ps;

            UpdateRoles(task);

            UpdateStatus(task);

            UpdateTaskUser(task);

            var deadLines = UpdateDeadlineDates(task);

            await _dbContext.SaveChangesAsync();

            return deadLines;


        }

        private async Task AddSystemComments(TaskInfo task, int processId, string compiler,
              string v1, string v2, string publisher, string us)
        {


            //update the system comment on changes
            if (((PublicationDate != null) && (task.PublicationDate != PublicationDate)) ||
                ((RepromatDate != null) && (task.RepromatDate != RepromatDate)) ||
                (task.AnnounceDate != AnnounceDate) ||
                (task.CommitDate != CommitToPrintDate) ||
                (task.CisDate != CISDate))
                await _commentsHelper.AddTaskSystemComment(NcneCommentType.DateChange, processId, CurrentUser.DisplayName, null,
                    null, null);

            if (!string.IsNullOrEmpty(Compiler) && task.TaskRole.Compiler != compiler)
                await _commentsHelper.AddTaskSystemComment(NcneCommentType.CompilerChange, processId, CurrentUser.DisplayName,
                    null, Compiler, null);
            if (!string.IsNullOrEmpty(Verifier1) && task.TaskRole.VerifierOne != v1)
                await _commentsHelper.AddTaskSystemComment(NcneCommentType.V1Change, processId, CurrentUser.DisplayName,
                    null, Verifier1, null);
            if (!string.IsNullOrEmpty(Verifier2) && task.TaskRole.VerifierTwo != v2)
                await _commentsHelper.AddTaskSystemComment(NcneCommentType.V2Change, processId, CurrentUser.DisplayName,
                    null, Verifier2, null);
            if (!string.IsNullOrEmpty(Publisher) && task.TaskRole.Publisher != publisher)
                await _commentsHelper.AddTaskSystemComment(NcneCommentType.PublisherChange, processId, CurrentUser.DisplayName,
                    null, Publisher, null);
        }


        private void UpdateStatus(TaskInfo task)
        {
            var v2 = task.TaskStage.FirstOrDefault(t => t.TaskStageTypeId == (int)NcneTaskStageType.V2);
            var v2Rework = task.TaskStage.FirstOrDefault(t => t.TaskStageTypeId == (int)NcneTaskStageType.V2_Rework);

            if (Verifier2 == null)
            {
                if (v2 != null) v2.Status = NcneTaskStageStatus.Inactive.ToString();
                if (v2Rework != null) v2Rework.Status = NcneTaskStageStatus.Inactive.ToString();
            }
            else
            {
                if (v2?.Status == NcneTaskStageStatus.Inactive.ToString())
                {
                    if (v2 != null) v2.Status = NcneTaskStageStatus.Open.ToString();
                }
                if (v2Rework?.Status == NcneTaskStageStatus.Inactive.ToString())
                {
                    if (v2Rework != null) v2Rework.Status = NcneTaskStageStatus.Open.ToString();
                }
            }
        }

        private void UpdateRoles(TaskInfo task)
        {
            task.TaskRole.Compiler = Compiler;
            task.TaskRole.VerifierOne = Verifier1;
            task.TaskRole.VerifierTwo = Verifier2;
            task.TaskRole.Publisher = Publisher;

            foreach (var taskStage in task.TaskStage.Where
                (s => s.Status != NcneTaskStageStatus.Completed.ToString()))
            {
                taskStage.AssignedUser = (NcneTaskStageType)taskStage.TaskStageTypeId switch
                {
                    NcneTaskStageType.With_Geodesy => this.Compiler,
                    NcneTaskStageType.With_SDRA => this.Compiler,
                    NcneTaskStageType.Specification => this.Compiler,
                    NcneTaskStageType.Compile => this.Compiler,
                    NcneTaskStageType.V1_Rework => this.Compiler,
                    NcneTaskStageType.V2_Rework => this.Compiler,
                    NcneTaskStageType.V1 => this.Verifier1,
                    NcneTaskStageType.V2 => this.Verifier2,
                    _ => this.Publisher
                };
            }
        }

        private void UpdateTaskUser(TaskInfo task)
        {
            var taskInProgress = task.TaskStage.Find(t => t.Status == NcneTaskStageStatus.InProgress.ToString()
                                                                 && t.TaskStageTypeId != (int)NcneTaskStageType.Forms);
            if (taskInProgress == null)
                task.AssignedUser = task.TaskRole.Publisher;
            else
            {
                task.AssignedUser = (NcneTaskStageType)taskInProgress.TaskStageTypeId switch
                {
                    NcneTaskStageType.With_SDRA => task.TaskRole.Compiler,
                    NcneTaskStageType.With_Geodesy => task.TaskRole.Compiler,
                    NcneTaskStageType.Specification => task.TaskRole.Compiler,
                    NcneTaskStageType.Compile => task.TaskRole.Compiler,
                    NcneTaskStageType.V1 => task.TaskRole.VerifierOne,
                    NcneTaskStageType.V1_Rework => task.TaskRole.Compiler,
                    NcneTaskStageType.V2 => task.TaskRole.VerifierTwo,
                    NcneTaskStageType.V2_Rework => task.TaskRole.Compiler,
                    _ => task.TaskRole.Publisher
                };
            }

        }

        private DeadlineId UpdateDeadlineDates(TaskInfo task)
        {

            DeadlineId result = new DeadlineId();

            //Update deadline dates in the taskStages
            foreach (var taskStage in task.TaskStage)
            {
                switch ((NcneTaskStageType)taskStage.TaskStageTypeId)
                {
                    case NcneTaskStageType.Forms:
                        {
                            taskStage.DateExpected = AnnounceDate;
                            result.FormsDate = taskStage.TaskStageId;
                            break;
                        }

                    case NcneTaskStageType.Commit_To_Print:
                        {
                            taskStage.DateExpected = CommitToPrintDate;
                            result.CommitDate = taskStage.TaskStageId;
                            break;
                        }

                    case NcneTaskStageType.CIS:
                        {
                            taskStage.DateExpected = CISDate;
                            result.CisDate = taskStage.TaskStageId;
                            break;
                        }

                    case NcneTaskStageType.Publication:
                        {
                            taskStage.DateExpected = PublicationDate;
                            result.PublishDate = taskStage.TaskStageId;
                            break;
                        }
                }
            }

            return result;

        }


        public async Task<JsonResult> OnGetUsersAsync()
        {

            var users =
                (await _ncneUserDbService.GetUsersFromDbAsync()).Select(u => new
                {
                    u.DisplayName,
                    u.UserPrincipalName
                });

            return new JsonResult(users);

        }

        public async Task<JsonResult> OnPostGetChartDetails(int versionNumber)
        {
            var panelInfo = _carisProjectHelper.GetValidHpdPanelInfo(versionNumber).Result;

            if (panelInfo.Item1 > 0)
            {
                var result = new[]
                {
                    panelInfo.Item1.ToString(),
                    panelInfo.Item2,
                    panelInfo.Item3.ToString(),
                    panelInfo.Item4
                };
                return new JsonResult(result);
            }
            else
            {
                return new JsonResult("Invalid Chart Version Number")
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }

        public async Task<JsonResult> OnPostPublishCarisChart(int versionNumber, int processId, int stageId, string userName)
        {
            try
            {


                var result = _carisProjectHelper.PublishCarisProject(versionNumber).Result;

                if (result)
                {
                    await CompleteStage(processId, stageId);

                    return new JsonResult(result)
                    { StatusCode = (int)HttpStatusCode.OK };
                }
                else
                {
                    return new JsonResult(result)
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }
            }
            catch (Exception e)
            {
                return new JsonResult(e.InnerException?.Message)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

        }

        public async Task<JsonResult> OnPostTaskCommentAsync(string txtComment, int commentProcessId)
        {
            txtComment = string.IsNullOrEmpty(txtComment) ? string.Empty : txtComment.Trim();

            if (!string.IsNullOrEmpty(txtComment))
            {
                await _commentsHelper.AddTaskComment(txtComment, commentProcessId, CurrentUser.DisplayName);
            }

            var result = new[]
            {
                CurrentUser.DisplayName,
                DateTime.Now.ToLongDateString()
            };
            return new JsonResult(result);
        }

        public async Task<JsonResult> OnPostStageCommentAsync(string txtComment, int commentProcessId, int stageId)
        {
            txtComment = string.IsNullOrEmpty(txtComment) ? string.Empty : txtComment.Trim();


            if (!string.IsNullOrEmpty(txtComment))
            {
                await _commentsHelper.AddTaskStageComment(txtComment, commentProcessId, stageId, CurrentUser.DisplayName);
                await _dbContext.SaveChangesAsync();
            }

            var result = new[]
            {
                CurrentUser.DisplayName,
                DateTime.Now.ToLongDateString()
            };
            return new JsonResult(result);

        }

    }
}




