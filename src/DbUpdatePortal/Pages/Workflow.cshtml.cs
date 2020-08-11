using Common.Helpers;
using Common.Helpers.Auth;
using DbUpdatePortal.Auth;
using DbUpdatePortal.Configuration;
using DbUpdatePortal.Enums;
using DbUpdatePortal.Helpers;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace DbUpdatePortal
{
    [TypeFilter(typeof(JavascriptError))]
    public class WorkflowModel : PageModel
    {
        private readonly DbUpdateWorkflowDbContext _dbContext;
        private readonly ILogger _logger;
        private readonly ICommentsHelper _commentsHelper;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly IDbUpdateUserDbService _dbUpdateUserDbService;
        private readonly IAdDirectoryService _adDirectoryService;
        //private readonly IWorkflowStageHelper _workflowStageHelper;
        public int ProcessId { get; set; }

        [DisplayName("Task Name")] [BindProperty] public string Name { get; set; }

        [DisplayName("Charting Area")] public string ChartingArea { get; set; }

        [DisplayName("Update Type")]
        [BindProperty]
        public string UpdateType { get; set; }


        [DisplayName("Product Action Required")]
        [BindProperty]
        public string ProductAction { get; set; }

        public bool IsReadOnly { get; set; }

        public string TaskStatus { get; set; }

        [DisplayName("Target date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        [BindProperty]
        public DateTime? TargetDate { get; set; }

        [DisplayName("Compiler")]
        [BindProperty]
        public AdUser Compiler { get; set; }

        public SelectList CompilerList { get; set; }

        [DisplayName("Verifier")]
        [BindProperty]
        public AdUser Verifier { get; set; }

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
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        [BindProperty]
        public List<TaskStage> TaskStages { get; set; }

        public WorkflowModel(DbUpdateWorkflowDbContext dbContext,
            ILogger<WorkflowModel> logger,
            ICommentsHelper commentsHelper,
            ICarisProjectHelper carisProjectHelper,
            IOptions<GeneralConfig> generalConfig,
            IPageValidationHelper pageValidationHelper,
            IDbUpdateUserDbService dbUpdateUserDbService,
            IAdDirectoryService adDirectoryService
            //,IWorkflowStageHelper workflowStageHelper
            )
        {
            _dbContext = dbContext;
            _logger = logger;
            _commentsHelper = commentsHelper;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;

            _pageValidationHelper = pageValidationHelper;
            _dbUpdateUserDbService = dbUpdateUserDbService;
            _adDirectoryService = adDirectoryService;
            //_workflowStageHelper = workflowStageHelper;

            ValidationErrorMessages = new List<string>();
        }

        //public async Task<IActionResult> OnPostTaskTerminateAsync(string comment, int processId)
        //{
        //    LogContext.PushProperty("ActivityName", "Workflow");
        //    LogContext.PushProperty("ProcessId", processId);
        //    LogContext.PushProperty("NCNEPortalResource", nameof(OnPostTaskTerminateAsync));
        //    LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

        //    LogContext.PushProperty("Comment", comment);

        //    _logger.LogInformation("Entering TaskTerminate for Workflow with: ProcessId: {ProcessId}; Comment: {Comment};");

        //    if (string.IsNullOrWhiteSpace(comment))
        //    {
        //        _logger.LogError("Comment is null, empty or whitespace: {Comment}");
        //        throw new ArgumentException($"{nameof(comment)} is null, empty or whitespace");
        //    }

        //    if (processId < 1)
        //    {
        //        _logger.LogError("ProcessId is less than 1: {ProcessId}");
        //        throw new ArgumentException($"{nameof(processId)} is less than 1");
        //    }

        //    _logger.LogInformation("Terminating with: ProcessId: {ProcessId}; Comment: {Comment};");

        //    var taskInfo = UpdateTaskAsTerminated(processId);

        //    var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);
        //    await _commentsHelper.AddTaskComment($"Terminate comment: {comment}", taskInfo.ProcessId, user);

        //    _logger.LogInformation("Terminated successfully with: ProcessId: {ProcessId}; Comment: {Comment};");

        //    return RedirectToPage("/Index");

        //}

        //private TaskInfo UpdateTaskAsTerminated(int processId)
        //{
        //    var taskInfo = _dbContext.TaskInfo.FirstOrDefault(t => t.ProcessId == processId);

        //    if (taskInfo == null)
        //    {
        //        _logger.LogError("ProcessId {ProcessId} does not appear in the TaskInfo table");
        //        throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the TaskInfo table");
        //    }

        //    taskInfo.Status = NcneTaskStatus.Terminated.ToString();
        //    taskInfo.StatusChangeDate = DateTime.Now;
        //    _dbContext.SaveChanges();

        //    return taskInfo;
        //}

        public void OnGetAsync(int processId)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnGetAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            _logger.LogInformation("Entering Get for Workflow with: ProcessId: {ProcessId}; ActivityName: {ActivityName}");

            ProcessId = processId;

            var taskInfo = _dbContext.TaskInfo
                .Include(task => task.TaskRole).ThenInclude(c => c.Compiler)
                .Include(task => task.TaskRole).ThenInclude(c => c.Verifier)
                .Include(task => task.TaskStage).ThenInclude(comment => comment.TaskStageComment).ThenInclude(u => u.AdUser)
                .Include(task => task.TaskStage).ThenInclude(stagetype => stagetype.TaskStageType)
                .Include(task => task.TaskStage).ThenInclude(r => r.Assigned)
                .Include(task => task.TaskComment).ThenInclude(c => c.AdUser)
                .FirstOrDefault(t => t.ProcessId == processId);


            Name = taskInfo.Name;
            ChartingArea = taskInfo.ChartingArea;
            UpdateType = taskInfo.UpdateType;
            ProductAction = taskInfo.ProductAction;

            TargetDate = taskInfo.TargetDate;

            Compiler = taskInfo.TaskRole.Compiler;
            Verifier = taskInfo.TaskRole.Verifier;

            TaskStages = taskInfo.TaskStage;


            var carisProject = _dbContext.CarisProjectDetails.FirstOrDefault(c => c.ProcessId == processId);

            TaskComments = taskInfo.TaskComment;

            if (carisProject != null)
            {
                CarisProjectName = carisProject.ProjectName;
                IsCarisProjectCreated = true;
            }
            else if (taskInfo != null) CarisProjectName = $"{taskInfo.Name}_{ProcessId}";

            Header = $"{taskInfo.Name}{(String.IsNullOrEmpty(taskInfo.ChartingArea) ? "" : $" - {taskInfo.ChartingArea}")}";

            //Enable complete if all stages are either completed or Inactive.
            CompleteEnabled = !TaskStages.Exists(t => t.Status == DbUpdateTaskStageStatus.Open.ToString()
                                                     || t.Status == DbUpdateTaskStageStatus.Rework.ToString()
                                                     || t.Status == DbUpdateTaskStageStatus.InProgress.ToString());



            IsReadOnly = taskInfo.Status == DbUpdateTaskStatus.Completed.ToString() ||
                         taskInfo.Status == DbUpdateTaskStatus.Terminated.ToString();
            if (!IsReadOnly)
            {
                Header += " - " + GetCurrentStage(TaskStages);
            }

            TaskStatus = taskInfo.Status;

            _logger.LogInformation("Finished Get for Workflow with: ProcessId: {ProcessId}; Action: {Action};");

        }

        //public async Task<IActionResult> OnPostCreateCarisProjectAsync(int processId, string projectName)
        //{
        //    LogContext.PushProperty("ActivityName", "Workflow");
        //    LogContext.PushProperty("ProcessId", processId);
        //    LogContext.PushProperty("NCNEPortalResource", nameof(OnPostCreateCarisProjectAsync));
        //    LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

        //    LogContext.PushProperty("ProjectName", projectName);

        //    _logger.LogInformation("Entering CreateCarisProject for Workflow with: ProcessId: {ProcessId} and Caris project name: {ProjectName}");

        //    var task = await _dbContext.TaskInfo.FindAsync(processId);

        //    if (string.IsNullOrWhiteSpace(task.ChartNumber))
        //    {
        //        throw new ArgumentException(" Please enter and save Chart Number before creating the Caris Project");
        //    }

        //    if (string.IsNullOrWhiteSpace(projectName))
        //    {
        //        throw new ArgumentException("Please provide a Caris Project Name.");
        //    }

        //    var projectId = await CreateCarisProject(processId, projectName);

        //    await UpdateCarisProjectDetails(processId, projectName, projectId);

        //    await _dbContext.SaveChangesAsync();

        //    _logger.LogInformation("Finished CreateCarisProject for Workflow with: ProcessId: {ProcessId} and Caris project name: {ProjectName}");

        //    return StatusCode(200);
        //}



        //private async Task<int> CreateCarisProject(int processId, string projectName)
        //{

        //    var carisProjectDetails =
        //        await _dbContext.CarisProjectDetails.FirstOrDefaultAsync(cp => cp.ProcessId == processId);

        //    if (carisProjectDetails != null)
        //    {
        //        return carisProjectDetails.ProjectId;
        //    }

        //    var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

        //    // which will also implicitly validate if the current user has been mapped to HPD account in our database
        //    var hpdUser = await GetHpdUser(user);

        //    _logger.LogInformation(
        //        "Creating Caris Project with ProcessId: {ProcessId}; ProjectName: {ProjectName}.");

        //    var projectId = await _carisProjectHelper.CreateCarisProject(processId, projectName,
        //        hpdUser.HpdUsername, _generalConfig.Value.CarisNcneProjectType,
        //        _generalConfig.Value.CarisNewProjectStatus,
        //        _generalConfig.Value.CarisNewProjectPriority, _generalConfig.Value.CarisProjectTimeoutSeconds);

        //    //Add the users from other roles to the Caris Project
        //    var role = await _dbContext.TaskRole.Include(c => c.Compiler)
        //                                        .Include(c => c.VerifierOne)
        //                                        .Include(c => c.VerifierTwo)
        //                                        .Include(c => c.HundredPercentCheck)
        //                                        .FirstOrDefaultAsync(t => t.ProcessId == processId);
        //    if (role.Compiler != null && role.Compiler != user)
        //    {
        //        hpdUser = await GetHpdUser(role.Compiler);
        //        await _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
        //             _generalConfig.Value.CarisProjectTimeoutSeconds);
        //    }
        //    if (role.VerifierOne != null && role.VerifierOne != user)
        //    {
        //        hpdUser = await GetHpdUser(role.VerifierOne);
        //        await _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
        //            _generalConfig.Value.CarisProjectTimeoutSeconds);
        //    }
        //    if (role.VerifierTwo != null && role.VerifierTwo != user)
        //    {
        //        hpdUser = await GetHpdUser(role.VerifierTwo);

        //        await _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
        //            _generalConfig.Value.CarisProjectTimeoutSeconds);
        //    }
        //    if (role.HundredPercentCheck != null && role.HundredPercentCheck != user)
        //    {
        //        hpdUser = await GetHpdUser(role.HundredPercentCheck);
        //        await _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
        //            _generalConfig.Value.CarisProjectTimeoutSeconds);
        //    }


        //    return projectId;
        //}

        //private async Task UpdateCarisProjectDetails(int processId, string projectName, int projectId)
        //{

        //    // If somehow the user has already created a project, remove it and create new row
        //    var toRemove = await _dbContext.CarisProjectDetails.Where(cp => cp.ProcessId == processId).ToListAsync();
        //    if (toRemove.Any())
        //    {
        //        _logger.LogInformation(
        //            "Removing the Caris project with ProcessId: {ProcessId}; ProjectName: {ProjectName}.");
        //        _dbContext.CarisProjectDetails.RemoveRange(toRemove);
        //        await _dbContext.SaveChangesAsync();
        //    }


        //    _logger.LogInformation(
        //        "Adding Caris Project with ProcessId: {ProcessId}; ProjectName: {ProjectName}. with new details");

        //    var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

        //    _dbContext.CarisProjectDetails.Add(new CarisProjectDetails
        //    {
        //        ProcessId = processId,
        //        Created = DateTime.Now,
        //        CreatedBy = user,
        //        ProjectId = projectId,
        //        ProjectName = projectName
        //    });

        //    await _dbContext.SaveChangesAsync();
        //}

        private async Task<HpdUser> GetHpdUser(AdUser user)
        {

            try
            {
                return await _dbContext.HpdUser.SingleAsync(u => u.AdUser == user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Unable to find HPD Username for {UserPrincipalName} in our system.");
                throw new InvalidOperationException($"Unable to find HPD username for {user.DisplayName}  in our system.",
                    ex.InnerException);
            }

        }



        public IActionResult OnPostValidateComplete(int processId, string username, int stageTypeId, bool publish)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostValidateComplete));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("AssignedUser", username);
            LogContext.PushProperty("StageTypeId", stageTypeId);


            _logger.LogInformation("Entering ValidateComplete for Workflow with: ProcessId: {ProcessId}, AssignedUser: {AssignedUser}, StageTypeId: {StageTypeId}, and Publish: {Publish}");

            ValidationErrorMessages.Clear();

            var task = _dbContext.TaskInfo.Single(t => t.ProcessId == processId);
            var roles = _dbContext.TaskRole
                .Include(c => c.Compiler)
                .Include(v => v.Verifier)
                .Single(r => r.ProcessId == processId);


            if (!(_pageValidationHelper.ValidateForCompletion(username, CurrentUser.UserPrincipalName,
                (DbUpdateTaskStageType)stageTypeId, roles, task.TargetDate,
                 ValidationErrorMessages)))
            {

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            _logger.LogInformation("Finished ValidateComplete for Workflow with: ProcessId: {ProcessId}, AssignedUser: {AssignedUser}, StageTypeId: {StageTypeId}, and Publish: {Publish}");

            return new JsonResult(HttpStatusCode.OK);
        }

        public IActionResult OnPostValidateRework(int processId, string username, int stageTypeId)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostValidateRework));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("AssignedUser", username);
            LogContext.PushProperty("StageTypeId", stageTypeId);

            _logger.LogInformation("Entering ValidateRework for Workflow with: ProcessId: {ProcessId}, AssignedUser: {AssignedUser}, and StageTypeId: {StageTypeId}");

            ValidationErrorMessages.Clear();

            if (!(_pageValidationHelper.ValidateForRework(username, CurrentUser.UserPrincipalName, ValidationErrorMessages)))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            _logger.LogInformation("Finished ValidateRework for Workflow with: ProcessId: {ProcessId}, AssignedUser: {AssignedUser}, and StageTypeId: {StageTypeId}");

            return new JsonResult(HttpStatusCode.OK);
        }

        //public IActionResult OnPostValidateCompleteWorkflow(string username)
        //{
        //    LogContext.PushProperty("ActivityName", "Workflow");
        //    LogContext.PushProperty("NCNEPortalResource", nameof(OnPostValidateCompleteWorkflow));
        //    LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

        //    LogContext.PushProperty("AssignedUser", username);

        //    _logger.LogInformation("Entering ValidateCompleteWorkflow for Workflow with: ProcessId: {ProcessId}, and AssignedUser: {AssignedUser}");

        //    ValidationErrorMessages.Clear();

        //    if (!(_pageValidationHelper.ValidateForCompleteWorkflow(username, CurrentUser.UserPrincipalName, ValidationErrorMessages)))
        //    {
        //        return new JsonResult(this.ValidationErrorMessages)
        //        {
        //            StatusCode = (int)HttpStatusCode.InternalServerError
        //        };
        //    }

        //    _logger.LogInformation("Finished ValidateCompleteWorkflow for Workflow with: ProcessId: {ProcessId}, and AssignedUser: {AssignedUser}");

        //    return new JsonResult(HttpStatusCode.OK);
        //}

        //public async Task<IActionResult> OnPostCompleteWorkflow(int processId)
        //{
        //    LogContext.PushProperty("ActivityName", "Workflow");
        //    LogContext.PushProperty("ProcessId", processId);
        //    LogContext.PushProperty("NCNEPortalResource", nameof(OnPostCompleteWorkflow));
        //    LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

        //    _logger.LogInformation("Entering CompleteWorkflow for Workflow with: ProcessId: {ProcessId}");

        //    var taskInfo = _dbContext.TaskInfo.FirstOrDefaultAsync(t => t.ProcessId == processId).Result;

        //    taskInfo.Status = NcneTaskStatus.Completed.ToString();
        //    taskInfo.StatusChangeDate = DateTime.Now;

        //    var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);
        //    await _commentsHelper.AddTaskSystemComment(NcneCommentType.CompleteWorkflow, processId, user, null, null, null);

        //    await _dbContext.SaveChangesAsync();

        //    _logger.LogInformation("Finished CompleteWorkflow for Workflow with: ProcessId: {ProcessId}");

        //    return new JsonResult(HttpStatusCode.OK);
        //}

        public async Task<IActionResult> OnPostCompleteAsync(int processId, int stageId, bool isRework)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostCompleteAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("StageId", stageId);
            LogContext.PushProperty("IsRework", isRework);

            _logger.LogInformation("Entering Complete for Workflow with: ProcessId: {ProcessId}, StageId: {StageId}, and IsRework: {IsRework}");

            if (isRework)
            {
                await SendtoRework(processId, stageId);
            }
            else
            {
                await CompleteStage(processId, stageId);
            }

            _logger.LogInformation("Finished Complete for Workflow with: ProcessId: {ProcessId}, StageId: {StageId}, and IsRework: {IsRework}");

            return new JsonResult(HttpStatusCode.OK);
        }

        private async Task<bool> SendtoRework(int processId, int stageId)
        {

            var taskStages = _dbContext.TaskStage.Include(r => r.Assigned)
                .Where(s => s.ProcessId == processId).Include(t => t.TaskStageType);

            var currentStage = await taskStages.SingleAsync(t => t.TaskStageId == stageId);
            var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

            currentStage.Status = DbUpdateTaskStageStatus.Rework.ToString();
            currentStage.DateCompleted = DateTime.Now;
            currentStage.Assigned = user;

            _logger.LogInformation(
                "Stage {StageId} of task {ProcessId} sent for rework.");

            var nextStage = DbUpdateTaskStageType.Verification_Rework;

            taskStages.First(t => t.TaskStageTypeId == (int)nextStage).Status =
                DbUpdateTaskStageStatus.InProgress.ToString();

            var nextStageUser = taskStages.FirstOrDefault(t => t.TaskStageTypeId == (int)nextStage)?
                .Assigned;

            var taskInfo = await _dbContext.TaskInfo.SingleAsync(t => t.ProcessId == processId);

            taskInfo.Assigned = nextStageUser;
            taskInfo.AssignedDate = DateTime.Now;
            taskInfo.CurrentStage = taskStages.FirstAsync(t => t.TaskStageTypeId == (int)nextStage).Result.TaskStageType.Name;

            var stageName = _dbContext.TaskStageType.SingleAsync(t => t.TaskStageTypeId == currentStage.TaskStageTypeId).Result.Name;

            await _commentsHelper.AddTaskSystemComment(DbUpdateCommentType.ReworkStage, processId, user, stageName, null, null);

            await _dbContext.SaveChangesAsync();


            return true;
        }

        private async Task<bool> CompleteStage(int processId, int stageId)
        {

            var taskStages = _dbContext.TaskStage.Include(r => r.Assigned)
                .Where(s => s.ProcessId == processId).Include(t => t.TaskStageType);

            var currentStage = await taskStages.SingleAsync(t => t.TaskStageId == stageId);

            var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

            currentStage.Status = DbUpdateTaskStageStatus.Completed.ToString();
            currentStage.DateCompleted = DateTime.Now;
            currentStage.Assigned = user;

            _logger.LogInformation(
                "Stage {StageId} of task {ProcessId} is completed.");


            //var v2 = await taskStages.SingleAsync(t => t.ProcessId == processId &&
            //                                t.TaskStageTypeId == (int)DbUpdateTaskStageType.V2);

            //bool v2Available = (v2.Status != DbUpdateTaskStageStatus.Inactive.ToString());


            var nextStage = (DbUpdateTaskStageType)currentStage.TaskStageTypeId switch
            {
                DbUpdateTaskStageType.Compile => (int)DbUpdateTaskStageType.Verify,
                DbUpdateTaskStageType.Verify => (int)DbUpdateTaskStageType.SNC,
                DbUpdateTaskStageType.Verification_Rework => (int)DbUpdateTaskStageType.Verify,
                DbUpdateTaskStageType.SNC => (int)DbUpdateTaskStageType.ENC,
                DbUpdateTaskStageType.ENC => 0,
                _ => throw new ArgumentOutOfRangeException()
            };

            var taskInfo = await _dbContext.TaskInfo.SingleAsync(t => t.ProcessId == processId);
            if (nextStage > 0)
            {
                taskStages.First(t => t.TaskStageTypeId == (int)nextStage).Status =
                    DbUpdateTaskStageStatus.InProgress.ToString();

                var nextStageUser = taskStages.FirstOrDefault(t => t.TaskStageTypeId == (int)nextStage)?
                    .Assigned;

                taskInfo.Assigned = nextStageUser;
                taskInfo.AssignedDate = DateTime.Now;
            }

            var stageName = _dbContext.TaskStageType.SingleAsync(t => t.TaskStageTypeId == currentStage.TaskStageTypeId).Result.Name;

            await _commentsHelper.AddTaskSystemComment(DbUpdateCommentType.CompleteStage,
                processId, user, stageName, null, null);

            taskInfo.CurrentStage = GetCurrentStage(new List<TaskStage>(taskStages));

            await _dbContext.SaveChangesAsync();

            return true;
        }

        private string GetCurrentStage(List<TaskStage> taskStages)
        {
            var inProgress = taskStages.FindAll(t => t.Status == DbUpdateTaskStageStatus.InProgress.ToString())
                .OrderBy(t => t.TaskStageTypeId);

            return inProgress.Any() ? inProgress.First().TaskStageType.Name : "Awaiting Completion";
        }

        //public async Task<IActionResult> OnPostSaveAsync(int processId, string chartType, string chartNo)
        //{
        //    LogContext.PushProperty("ActivityName", "Workflow");
        //    LogContext.PushProperty("ProcessId", processId);
        //    LogContext.PushProperty("NCNEPortalResource", nameof(OnPostSaveAsync));
        //    LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

        //    LogContext.PushProperty("ChartType", chartType);
        //    LogContext.PushProperty("ChartNo", chartNo);

        //    _logger.LogInformation("Entering Save for Workflow with: ProcessId: {ProcessId}, ChartType: {ChartType}, and ChartNo: {ChartNo}");

        //    ValidationErrorMessages.Clear();

        //    ChartNo = chartNo;
        //    var role = new TaskRole()
        //    {
        //        ProcessId = processId,
        //        Compiler = string.IsNullOrEmpty(Compiler?.UserPrincipalName) ? null : await _dbUpdateUserDbService.GetAdUserAsync(Compiler.UserPrincipalName),
        //        VerifierOne = string.IsNullOrEmpty(Verifier1?.UserPrincipalName) ? null : await _dbUpdateUserDbService.GetAdUserAsync(Verifier1.UserPrincipalName),
        //        VerifierTwo = string.IsNullOrEmpty(Verifier2?.UserPrincipalName) ? null : await _dbUpdateUserDbService.GetAdUserAsync(Verifier2.UserPrincipalName),
        //        HundredPercentCheck = string.IsNullOrEmpty(HundredPercentCheck?.UserPrincipalName) ? null : await _dbUpdateUserDbService.GetAdUserAsync(HundredPercentCheck.UserPrincipalName)

        //    };
        //    var ThreePSInfo = (SentTo3Ps, SendDate3ps, ExpectedReturnDate3ps, ActualReturnDate3ps);


        //    if (!(_pageValidationHelper.ValidateWorkflowPage(role, PublicationDate, RepromatDate, Dating, chartType,
        //        ThreePSInfo,
        //        ValidationErrorMessages)))
        //    {

        //        return new JsonResult(this.ValidationErrorMessages)
        //        {
        //            StatusCode = (int)HttpStatusCode.InternalServerError
        //        };
        //    }

        //    var result = await UpdateTaskInformation(processId, chartType, role);

        //    _logger.LogInformation("Finished Save for Workflow with: ProcessId: {ProcessId}, ChartType: {ChartType}, and ChartNo: {ChartNo}");


        //    return new JsonResult(JsonConvert.SerializeObject(result));
        //}

        //private async Task<DeadlineId> UpdateTaskInformation(int processId, string chartType, TaskRole role)
        //{

        //    _logger.LogInformation(
        //        " Updating Task Information for process {ProcessId}.");


        //    var task =
        //        await _dbContext.TaskInfo
        //              .Include(t => t.TaskRole)
        //              .ThenInclude(c => c.Compiler)
        //              .Include(t => t.TaskRole)
        //              .ThenInclude(v => v.VerifierOne)
        //              .Include(t => t.TaskRole)
        //              .ThenInclude(v => v.VerifierTwo)
        //              .Include(t => t.TaskRole)
        //              .ThenInclude(h => h.HundredPercentCheck)
        //              .Include(s => s.TaskStage)
        //              .ThenInclude(r => r.Assigned)
        //              .FirstAsync(t => t.ProcessId == processId);
        //    task.Ion = Ion;
        //    task.ChartNumber = ChartNo;
        //    task.Duration = Enum.GetName(typeof(DeadlineEnum), Dating);

        //    if (chartType == "Adoption" && RepromatDate != null)
        //    {

        //        PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)RepromatDate);

        //    }

        //    await AddSystemComments(task, processId, role);

        //    task.RepromatDate = RepromatDate;

        //    task.PublicationDate = PublicationDate;
        //    task.AnnounceDate = AnnounceDate;
        //    task.CommitDate = CommitToPrintDate;
        //    task.CisDate = CISDate;
        //    task.Country = Country;
        //    task.ThreePs = SentTo3Ps;
        //    task.SentDate3Ps = SendDate3ps;
        //    task.ExpectedDate3Ps = ExpectedReturnDate3ps;
        //    task.ActualDate3Ps = ActualReturnDate3ps;

        //    var carisProject = _dbContext.CarisProjectDetails.FirstOrDefault(c => c.ProcessId == task.ProcessId);

        //    if (carisProject != null)
        //        UpdateCarisProjectUsers(task, role, carisProject.ProjectId);

        //    UpdateRoles(task, role);

        //    UpdateStatus(task, role);

        //    UpdateTaskUser(task, role);

        //    var deadLines = UpdateDeadlineDates(task);

        //    await _dbContext.SaveChangesAsync();

        //    return deadLines;


        //}

        //private async Task AddSystemComments(TaskInfo task, int processId, TaskRole role)
        //{

        //    _logger.LogInformation("Adding system comments for process {ProcessId}.");

        //    var currentUser = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

        //    //update the system comment on changes
        //    if (((PublicationDate != null) && (task.PublicationDate != PublicationDate)) ||
        //        ((RepromatDate != null) && (task.RepromatDate != RepromatDate)) ||
        //        (task.AnnounceDate != AnnounceDate) ||
        //        (task.CommitDate != CommitToPrintDate) ||
        //        (task.CisDate != CISDate))
        //        await _commentsHelper.AddTaskSystemComment(NcneCommentType.DateChange, processId, currentUser, null,
        //            null, null);

        //    if (role.Compiler != null && task.TaskRole?.Compiler != role.Compiler)
        //        await _commentsHelper.AddTaskSystemComment(NcneCommentType.CompilerChange, processId, currentUser,
        //            null, role.Compiler.DisplayName, null);
        //    if (role.VerifierOne != null && task.TaskRole?.VerifierOne != role.VerifierOne)
        //        await _commentsHelper.AddTaskSystemComment(NcneCommentType.V1Change, processId, currentUser,
        //            null, role.VerifierOne.DisplayName, null);
        //    if (role.VerifierTwo != null && task.TaskRole?.VerifierTwo != role.VerifierTwo)
        //        await _commentsHelper.AddTaskSystemComment(NcneCommentType.V2Change, processId, currentUser,
        //            null, role.VerifierTwo.DisplayName, null);
        //    if (role.HundredPercentCheckAdUserId != null && task.TaskRole?.HundredPercentCheck != role.HundredPercentCheck)
        //        await _commentsHelper.AddTaskSystemComment(NcneCommentType.HundredPcChange, processId, currentUser,
        //            null, role.HundredPercentCheck.DisplayName, null);

        //    if ((task.ThreePs != SentTo3Ps) || (task.SentDate3Ps != SendDate3ps)
        //                                    || (task.ExpectedDate3Ps != ExpectedReturnDate3ps) ||
        //                                    (task.ActualDate3Ps != ActualReturnDate3ps))
        //    {
        //        await _commentsHelper.AddTaskSystemComment(NcneCommentType.ThreePsChange, processId,
        //            currentUser,
        //            null, null, null);
        //    }
        //}

        //private void UpdateStatus(TaskInfo task, TaskRole role)
        //{

        //    _logger.LogInformation("Updating task stage status for task {ProcessId}.");


        //    var v2 = task.TaskStage.FirstOrDefault(t => t.TaskStageTypeId == (int)NcneTaskStageType.V2);
        //    var v2Rework = task.TaskStage.FirstOrDefault(t => t.TaskStageTypeId == (int)NcneTaskStageType.V2_Rework);

        //    if (role.VerifierTwo == null)
        //    {
        //        if (v2 != null) v2.Status = NcneTaskStageStatus.Inactive.ToString();
        //        if (v2Rework != null) v2Rework.Status = NcneTaskStageStatus.Inactive.ToString();
        //    }
        //    else
        //    {
        //        if (v2?.Status == NcneTaskStageStatus.Inactive.ToString())
        //        {
        //            if (v2 != null) v2.Status = NcneTaskStageStatus.Open.ToString();
        //        }
        //        if (v2Rework?.Status == NcneTaskStageStatus.Inactive.ToString())
        //        {
        //            if (v2Rework != null) v2Rework.Status = NcneTaskStageStatus.Open.ToString();
        //        }
        //    }
        //}

        //private void UpdateCarisProjectUsers(TaskInfo task, TaskRole role, int projectId)
        //{


        //    var hpdUser = GetHpdUser(role.Compiler).Result;

        //    if (role.Compiler != null && role.Compiler != task.TaskRole.Compiler)
        //        _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
        //            _generalConfig.Value.CarisProjectTimeoutSeconds);

        //    if (role.VerifierOne != null && role.VerifierOne != task.TaskRole.VerifierOne)
        //    {
        //        hpdUser = GetHpdUser(role.VerifierOne).Result;
        //        _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
        //            _generalConfig.Value.CarisProjectTimeoutSeconds);
        //    }

        //    if (role.VerifierTwo != null && role.VerifierTwo != task.TaskRole.VerifierTwo)
        //    {
        //        hpdUser = GetHpdUser(role.VerifierTwo).Result;
        //        _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
        //            _generalConfig.Value.CarisProjectTimeoutSeconds);
        //    }

        //    if (role.HundredPercentCheck != null &&
        //        role.HundredPercentCheck != task.TaskRole.HundredPercentCheck)
        //    {
        //        hpdUser = GetHpdUser(role.HundredPercentCheck).Result;
        //        _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
        //            _generalConfig.Value.CarisProjectTimeoutSeconds);
        //    }


        //}

        //private void UpdateRoles(TaskInfo task, TaskRole role)
        //{

        //    _logger.LogInformation("Updating Roles for for task {ProcessId}.");


        //    task.TaskRole = role;

        //    foreach (var taskStage in task.TaskStage.Where
        //        (s => s.Status != NcneTaskStageStatus.Completed.ToString()))
        //    {
        //        taskStage.Assigned = (NcneTaskStageType)taskStage.TaskStageTypeId switch
        //        {
        //            NcneTaskStageType.With_Geodesy => role.Compiler,
        //            NcneTaskStageType.With_SDRA => role.Compiler,
        //            NcneTaskStageType.Specification => role.Compiler,
        //            NcneTaskStageType.Compile => role.Compiler,
        //            NcneTaskStageType.V1_Rework => role.Compiler,
        //            NcneTaskStageType.V2_Rework => role.Compiler,
        //            NcneTaskStageType.V2 => role.VerifierTwo,
        //            NcneTaskStageType.Hundred_Percent_Check => role.HundredPercentCheck,
        //            _ => role.VerifierOne
        //        };
        //    }
        //}

        //private void UpdateTaskUser(TaskInfo task, TaskRole role)
        //{

        //    _logger.LogInformation("Updating Task stage users from roles for task {ProcessId}.");

        //    var taskInProgress = task.TaskStage.Find(t => t.Status == NcneTaskStageStatus.InProgress.ToString()
        //                                                         && t.TaskStageTypeId != (int)NcneTaskStageType.Forms);
        //    if (taskInProgress == null)
        //        task.Assigned = role.HundredPercentCheck;
        //    else
        //    {
        //        task.Assigned = (NcneTaskStageType)taskInProgress.TaskStageTypeId switch
        //        {
        //            NcneTaskStageType.With_SDRA => role.Compiler,
        //            NcneTaskStageType.With_Geodesy => role.Compiler,
        //            NcneTaskStageType.Specification => role.Compiler,
        //            NcneTaskStageType.Compile => role.Compiler,
        //            NcneTaskStageType.V1_Rework => role.Compiler,
        //            NcneTaskStageType.V2_Rework => role.Compiler,
        //            NcneTaskStageType.V2 => role.VerifierTwo,
        //            NcneTaskStageType.Hundred_Percent_Check => role.HundredPercentCheck,
        //            _ => role.VerifierOne
        //        };
        //    }

        //}


        public async Task<JsonResult> OnGetUsersAsync()
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("NCNEPortalResource", nameof(OnGetUsersAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            _logger.LogInformation("Entering GetUsers for Workflow");

            var users =
                (await _dbUpdateUserDbService.GetUsersFromDbAsync()).Select(u => new
                {
                    u.DisplayName,
                    u.UserPrincipalName
                });

            _logger.LogInformation("Finished GetUsers for Workflow");

            return new JsonResult(users);
        }

        public async Task<JsonResult> OnPostGetChartDetails(int versionNumber)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("NCNEPortalResource", nameof(OnPostGetChartDetails));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("VersionNumber", versionNumber);

            _logger.LogInformation("Entering GetChartDetails for Workflow with: VersionNumber: {VersionNumber}");

            var panelInfo = await _carisProjectHelper.GetValidHpdPanelInfo(versionNumber);

            if (string.IsNullOrEmpty(panelInfo.Item1))
            {
                _logger.LogError("Invalid chart version number {VersionNumber} entered for publish chart.");

                return new JsonResult("Invalid Chart Version Number")
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            var result = new[]
            {
                panelInfo.Item1,
                panelInfo.Item2,
                panelInfo.Item3.ToString(),
                panelInfo.Item4
            };

            _logger.LogInformation("Finished GetChartDetails for Workflow with: VersionNumber: {VersionNumber}");

            return new JsonResult(result);
        }

        //public async Task<JsonResult> OnPostPublishCarisChart(int versionNumber, int processId, int stageId)
        //{
        //    LogContext.PushProperty("ActivityName", "Workflow");
        //    LogContext.PushProperty("ProcessId", processId);
        //    LogContext.PushProperty("NCNEPortalResource", nameof(OnPostPublishCarisChart));
        //    LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

        //    LogContext.PushProperty("VersionNumber", versionNumber);
        //    LogContext.PushProperty("StageId", stageId);

        //    _logger.LogInformation("Entering PublishCarisChart for Workflow with: ProcessId: {ProcessId}, VersionNumber: {VersionNumber}, and StageId: {StageId}");

        //    try
        //    {

        //        var result = _carisProjectHelper.PublishCarisProject(versionNumber).Result;

        //        if (result)
        //        {
        //            await CompleteStage(processId, stageId);

        //            _logger.LogInformation("Caris chart with version {VersionNumber} published for task {ProcessId}.");

        //            _logger.LogInformation("Finished PublishCarisChart for Workflow with: ProcessId: {ProcessId}, VersionNumber: {VersionNumber}, and StageId: {StageId}");

        //            return new JsonResult(result)
        //            { StatusCode = (int)HttpStatusCode.OK };
        //        }
        //        else
        //        {
        //            _logger.LogError("Error publishing Caris chart with version {VersionNumber} for task {ProcessId}.");


        //            return new JsonResult(result)
        //            {
        //                StatusCode = (int)HttpStatusCode.InternalServerError
        //            };
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e,
        //            "Publishing Caris chart with version {VersionNumber} for task {ProcessId} failed with error: " + e.InnerException?.Message);

        //        return new JsonResult(e.InnerException?.Message)
        //        {
        //            StatusCode = (int)HttpStatusCode.InternalServerError
        //        };
        //    }

        //}

        public async Task<JsonResult> OnPostTaskCommentAsync(string txtComment, int commentProcessId)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", commentProcessId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostTaskCommentAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("Comment", txtComment);

            _logger.LogInformation("Entering TaskComment for Workflow with: ProcessId: {ProcessId}, and Comment: {Comment}");

            txtComment = string.IsNullOrEmpty(txtComment) ? string.Empty : txtComment.Trim();

            if (!string.IsNullOrEmpty(txtComment))
            {
                _logger.LogInformation("Task comment added for task {ProcessId}.");

                var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);
                await _commentsHelper.AddTaskComment(txtComment, commentProcessId, user);
            }

            var result = new[]
            {
                CurrentUser.DisplayName,
                DateTime.Now.ToLongDateString()
            };

            _logger.LogInformation("Finished TaskComment for Workflow with: ProcessId: {ProcessId}, and Comment: {Comment}");

            return new JsonResult(result);
        }

        public async Task<JsonResult> OnPostStageCommentAsync(string txtComment, int commentProcessId, int stageId)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", commentProcessId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostStageCommentAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("Comment", txtComment);
            LogContext.PushProperty("StageId", stageId);

            _logger.LogInformation("Entering StageComment for Workflow with: ProcessId: {ProcessId}, StageId: {StageId}, and Comment: {Comment}");

            txtComment = string.IsNullOrEmpty(txtComment) ? string.Empty : txtComment.Trim();
            var currentUser = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

            if (!string.IsNullOrEmpty(txtComment))
            {
                _logger.LogInformation("Task stage comment added for task {ProcessId} and stage {StageId}.");

                await _commentsHelper.AddTaskStageComment(txtComment, commentProcessId, stageId, currentUser);
                await _dbContext.SaveChangesAsync();
            }

            var result = new[]
            {
                CurrentUser.DisplayName,
                DateTime.Now.ToLongDateString()
            };

            _logger.LogInformation("Finished StageComment for Workflow with: ProcessId: {ProcessId}, StageId: {StageId}, and Comment: {Comment}");

            return new JsonResult(result);

        }

    }
}




