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
using Newtonsoft.Json;
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

        public int ProcessId { get; set; }

        [DisplayName("Task Name")] [BindProperty] public string Name { get; set; }

        [DisplayName("Charting Area")] public string ChartingArea { get; set; }

        [DisplayName("Update Type")]
        [BindProperty]
        public string UpdateType { get; set; }


        [DisplayName("Product Action Required")]
        [BindProperty]
        public string ProductAction { get; set; }

        public SelectList ProductActions { get; set; }

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

        public bool VerifyCompleted { get; set; }

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


            ValidationErrorMessages = new List<string>();
        }

        public async Task<IActionResult> OnPostTaskTerminateAsync(string comment, int processId)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostTaskTerminateAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("Comment", comment);

            _logger.LogInformation(
                "Entering TaskTerminate for Workflow with: ProcessId: {ProcessId}; Comment: {Comment};");

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

            _logger.LogInformation("Terminating with: ProcessId: {ProcessId}; Comment: {Comment};");

            var taskInfo = UpdateTaskAsTerminated(processId);

            var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);
            await _commentsHelper.AddTaskComment($"Terminate comment: {comment}", taskInfo.ProcessId, user);

            //Mark the Caris project as complete if its already created
            var carisProject = _dbContext.CarisProjectDetails.FirstOrDefault(c => c.ProcessId == processId);

            if (carisProject != null)
                await _carisProjectHelper.MarkCarisProjectAsComplete(carisProject.ProjectId,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);


            _logger.LogInformation("Terminated successfully with: ProcessId: {ProcessId}; Comment: {Comment};");

            return RedirectToPage("/Index");

        }

        private TaskInfo UpdateTaskAsTerminated(int processId)
        {
            var taskInfo = _dbContext.TaskInfo.FirstOrDefault(t => t.ProcessId == processId);

            if (taskInfo == null)
            {
                _logger.LogError("ProcessId {ProcessId} does not appear in the TaskInfo table");
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the TaskInfo table");
            }

            taskInfo.Status = DbUpdateTaskStatus.Terminated.ToString();
            taskInfo.StatusChangeDate = DateTime.Now;
            _dbContext.SaveChanges();

            return taskInfo;
        }

        public async Task OnGetAsync(int processId)
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

            //Enable complete if Verify is completed and ENC and SNC are either completed ord Inactive 
            CompleteEnabled = !TaskStages.Exists(t =>
                (t.TaskStageTypeId == (int)DbUpdateTaskStageType.Verify &&
                 t.Status != DbUpdateTaskStageStatus.Completed.ToString()) ||
                ((t.TaskStageTypeId == (int)DbUpdateTaskStageType.Awaiting_Publication &&
                 t.Status == DbUpdateTaskStageStatus.Open.ToString()) ||
                 (t.TaskStageTypeId == (int)DbUpdateTaskStageType.Awaiting_Publication &&
            t.Status == DbUpdateTaskStageStatus.InProgress.ToString())));

            VerifyCompleted = TaskStages.Exists(t => t.TaskStageTypeId == (int)DbUpdateTaskStageType.Verify &&
                                                     t.Status == DbUpdateTaskStageStatus.Completed.ToString());

            IsReadOnly = taskInfo.Status == DbUpdateTaskStatus.Completed.ToString() ||
                         taskInfo.Status == DbUpdateTaskStatus.Terminated.ToString();
            if (!IsReadOnly)
            {
                Header += " - " + GetCurrentStage(TaskStages);
            }

            TaskStatus = taskInfo.Status;

            var productActions = await _dbContext.ProductAction.OrderBy(i => i.ProductActionId).Select(st => st.Name)
                .ToListAsync().ConfigureAwait(false);

            ProductActions = new SelectList(productActions);

            _logger.LogInformation("Finished Get for Workflow with: ProcessId: {ProcessId}; Action: {Action};");

        }

        public async Task<IActionResult> OnPostCreateCarisProjectAsync(int processId, string projectName)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostCreateCarisProjectAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("ProjectName", projectName);

            _logger.LogInformation("Entering CreateCarisProject for Workflow with: ProcessId: {ProcessId} and Caris project name: {ProjectName}");

            var task = await _dbContext.TaskInfo.FindAsync(processId);

            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Please provide a Caris Project Name.");
            }

            var projectId = await CreateCarisProject(processId, projectName);

            await UpdateCarisProjectDetails(processId, projectName, projectId);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Finished CreateCarisProject for Workflow with: ProcessId: {ProcessId} and Caris project name: {ProjectName}");

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

            var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

            // which will also implicitly validate if the current user has been mapped to HPD account in our database
            var hpdUser = await GetHpdUser(user);

            _logger.LogInformation(
                "Creating Caris Project with ProcessId: {ProcessId}; ProjectName: {ProjectName}.");

            var projectId = await _carisProjectHelper.CreateCarisProject(processId, projectName,
                hpdUser.HpdUsername, _generalConfig.Value.CarisDbUpdateProjectType,
                _generalConfig.Value.CarisNewProjectStatus,
                _generalConfig.Value.CarisNewProjectPriority, _generalConfig.Value.CarisProjectTimeoutSeconds);

            //Add the users from other roles to the Caris Project
            var role = await _dbContext.TaskRole.Include(c => c.Compiler)
                                                .Include(c => c.Verifier)
                                                .FirstOrDefaultAsync(t => t.ProcessId == processId);
            if (role.Compiler != null && role.Compiler != user)
            {
                hpdUser = await GetHpdUser(role.Compiler);
                await _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
                     _generalConfig.Value.CarisProjectTimeoutSeconds);
            }
            if (role.Verifier != null && role.Verifier != user)
            {
                hpdUser = await GetHpdUser(role.Verifier);
                await _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);
            }
            return projectId;
        }

        private async Task UpdateCarisProjectDetails(int processId, string projectName, int projectId)
        {

            // If somehow the user has already created a project, remove it and create new row
            var toRemove = await _dbContext.CarisProjectDetails.Where(cp => cp.ProcessId == processId).ToListAsync();
            if (toRemove.Any())
            {
                _logger.LogInformation(
                    "Removing the Caris project with ProcessId: {ProcessId}; ProjectName: {ProjectName}.");
                _dbContext.CarisProjectDetails.RemoveRange(toRemove);
                await _dbContext.SaveChangesAsync();
            }


            _logger.LogInformation(
                "Adding Caris Project with ProcessId: {ProcessId}; ProjectName: {ProjectName}. with new details");

            var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

            _dbContext.CarisProjectDetails.Add(new CarisProjectDetails
            {
                ProcessId = processId,
                Created = DateTime.Now,
                CreatedBy = user,
                ProjectId = projectId,
                ProjectName = projectName
            });

            await _dbContext.SaveChangesAsync();
        }

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



        public IActionResult OnPostValidateComplete(int processId, string username, int stageTypeId)
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

            _logger.LogInformation("Finished ValidateComplete for Workflow with: ProcessId: {ProcessId}, AssignedUser: {AssignedUser} and  StageTypeId: {StageTypeId}");

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

        public IActionResult OnPostValidateTerminateWorkflow(string username)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostValidateTerminateWorkflow));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("AssignedUser", username);
            _logger.LogInformation("Entering ValidateTerminateWorkflow for Workflow with: ProcessId: {ProcessId}, and AssignedUser: {AssignedUser}");

            ValidationErrorMessages.Clear();

            if (!(_pageValidationHelper.ValidateForTerminateWorkflow(username, CurrentUser.UserPrincipalName, ValidationErrorMessages)))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            _logger.LogInformation("Finished ValidateTerminateWorkflow for Workflow with: ProcessId: {ProcessId}, and AssignedUser: {AssignedUser}");

            return new JsonResult(HttpStatusCode.OK);
        }

        public IActionResult OnPostValidateCompleteWorkflow(string username)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostValidateCompleteWorkflow));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("AssignedUser", username);

            _logger.LogInformation("Entering ValidateCompleteWorkflow for Workflow with: ProcessId: {ProcessId}, and AssignedUser: {AssignedUser}");

            ValidationErrorMessages.Clear();

            if (!(_pageValidationHelper.ValidateForCompleteWorkflow(username, CurrentUser.UserPrincipalName, ValidationErrorMessages)))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            _logger.LogInformation("Finished ValidateCompleteWorkflow for Workflow with: ProcessId: {ProcessId}, and AssignedUser: {AssignedUser}");

            return new JsonResult(HttpStatusCode.OK);
        }

        public async Task<IActionResult> OnPostCompleteWorkflow(int processId)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostCompleteWorkflow));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            _logger.LogInformation("Entering CompleteWorkflow for Workflow with: ProcessId: {ProcessId}");

            var taskInfo = _dbContext.TaskInfo.FirstOrDefaultAsync(t => t.ProcessId == processId).Result;

            taskInfo.Status = DbUpdateTaskStatus.Completed.ToString();
            taskInfo.StatusChangeDate = DateTime.Now;

            var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);
            await _commentsHelper.AddTaskSystemComment(DbUpdateCommentType.CompleteWorkflow, processId, user, null, null, null);

            //Mark the Caris project as complete if its already created
            var carisProject = _dbContext.CarisProjectDetails.FirstOrDefault(c => c.ProcessId == processId);

            if (carisProject != null)
                await _carisProjectHelper.MarkCarisProjectAsComplete(carisProject.ProjectId,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);


            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Finished CompleteWorkflow for Workflow with: ProcessId: {ProcessId}");

            return new JsonResult(HttpStatusCode.OK);
        }

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


            var taskInfo = await _dbContext.TaskInfo.SingleAsync(t => t.ProcessId == processId);

            if (!Enum.TryParse(taskInfo.ProductAction, true, out DbUpdateProductAction selectedAction))
            {
                selectedAction = DbUpdateProductAction.Both;
            }

            var nextStageTypeId = (DbUpdateTaskStageType)currentStage.TaskStageTypeId switch
            {
                DbUpdateTaskStageType.Compile => (int)DbUpdateTaskStageType.Verify,
                DbUpdateTaskStageType.Verify =>
                selectedAction switch
                {
                    DbUpdateProductAction.None => 0,
                    DbUpdateProductAction.SNC => (int)DbUpdateTaskStageType.SNC,
                    _ => (int)DbUpdateTaskStageType.ENC
                }
                ,
                DbUpdateTaskStageType.Verification_Rework => (int)DbUpdateTaskStageType.Verify,
                DbUpdateTaskStageType.ENC =>
                selectedAction switch
                {
                    DbUpdateProductAction.ENC => (int)DbUpdateTaskStageType.Awaiting_Publication,
                    DbUpdateProductAction.SNC => (int)DbUpdateTaskStageType.SNC,
                    DbUpdateProductAction.Both => (int)DbUpdateTaskStageType.SNC,
                    _ => 0
                },
                DbUpdateTaskStageType.SNC => (int)DbUpdateTaskStageType.Awaiting_Publication,
                DbUpdateTaskStageType.Awaiting_Publication => 0,
                _ => throw new ArgumentOutOfRangeException()
            };


            if (nextStageTypeId > 0)
            {

                var nextStage = taskStages.First(t => t.TaskStageTypeId == (int)nextStageTypeId);

                if (nextStage.Status != DbUpdateTaskStageStatus.Inactive.ToString())
                {
                    nextStage.Status =
                        DbUpdateTaskStageStatus.InProgress.ToString();
                    taskInfo.Assigned = nextStage.Assigned;
                    taskInfo.AssignedDate = DateTime.Now;
                }

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

        public async Task<IActionResult> OnPostSaveAsync(int processId, string productAction)
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("dbUpdatePortalResource", nameof(OnPostSaveAsync));
            LogContext.PushProperty("UserPrincipalName", CurrentUser.UserPrincipalName);

            LogContext.PushProperty("ProductAction", productAction);


            _logger.LogInformation("Entering Save for Workflow with: ProcessId: {ProcessId}, ChartType: {ChartType}, and ChartNo: {ChartNo}");

            ValidationErrorMessages.Clear();

            ProductAction = productAction;
            var role = new TaskRole()
            {
                ProcessId = processId,
                Compiler = string.IsNullOrEmpty(Compiler?.UserPrincipalName) ? null : await _dbUpdateUserDbService.GetAdUserAsync(Compiler.UserPrincipalName),
                Verifier = string.IsNullOrEmpty(Verifier?.UserPrincipalName) ? null : await _dbUpdateUserDbService.GetAdUserAsync(Verifier.UserPrincipalName)
            };


            if (!(_pageValidationHelper.ValidateWorkflowPage(role, productAction, TargetDate, ValidationErrorMessages)))
            {

                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            var result = await UpdateTaskInformation(processId, productAction, role);

            _logger.LogInformation("Finished Save for Workflow with: ProcessId: {ProcessId}, ChartType: {ChartType}, and ChartNo: {ChartNo}");


            return new JsonResult(JsonConvert.SerializeObject(result));
        }

        private async Task<IActionResult> UpdateTaskInformation(int processId, string productAction, TaskRole role)
        {

            _logger.LogInformation(
                " Updating Task Information for process {ProcessId}.");


            var task =
                await _dbContext.TaskInfo
                      .Include(t => t.TaskRole)
                      .ThenInclude(c => c.Compiler)
                      .Include(t => t.TaskRole)
                      .ThenInclude(v => v.Verifier)
                      .Include(s => s.TaskStage)
                      .ThenInclude(r => r.Assigned)
                      .FirstAsync(t => t.ProcessId == processId);


            await AddSystemComments(task, processId, role);

            task.TargetDate = TargetDate;
            if (task.ProductAction != productAction)
            {
                UpdateTaskStageStatus(task, productAction);
                task.ProductAction = productAction;
            }



            var carisProject = _dbContext.CarisProjectDetails.FirstOrDefault(c => c.ProcessId == task.ProcessId);

            if (carisProject != null)
                UpdateCarisProjectUsers(task, role, carisProject.ProjectId);

            UpdateRoles(task, role);

            UpdateTaskUser(task, role);

            await _dbContext.SaveChangesAsync();

            return null;


        }

        private async Task AddSystemComments(TaskInfo task, int processId, TaskRole role)
        {

            _logger.LogInformation("Adding system comments for process {ProcessId}.");

            var currentUser = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

            //update the system comment on changes
            if (TargetDate != null && task.TargetDate != TargetDate)
                await _commentsHelper.AddTaskSystemComment(DbUpdateCommentType.DateChange, processId, currentUser, null,
                    null, TargetDate);

            if (role.Compiler != null && task.TaskRole?.Compiler != role.Compiler)
                await _commentsHelper.AddTaskSystemComment(DbUpdateCommentType.CompilerChange, processId, currentUser,
                    null, role.Compiler.DisplayName, null);

            if (role.Verifier != null && task.TaskRole?.Verifier != role.Verifier)
                await _commentsHelper.AddTaskSystemComment(DbUpdateCommentType.V1Change, processId, currentUser,
                    null, role.Verifier.DisplayName, null);

        }

        private void UpdateTaskStageStatus(TaskInfo task, string productAction)
        {
            if (!Enum.TryParse(this.ProductAction, true, out DbUpdateProductAction selectedAction))
            {
                selectedAction = DbUpdateProductAction.Both;
            }

            foreach (var stage in task.TaskStage)
            {
                //Assign the status of the task stage 
                switch ((DbUpdateTaskStageType)stage.TaskStageTypeId)
                {
                    case DbUpdateTaskStageType.Compile:
                        break;
                    case DbUpdateTaskStageType.Verify:
                        break;
                    case DbUpdateTaskStageType.Verification_Rework:
                        break;
                    case DbUpdateTaskStageType.ENC:
                        stage.Status = (selectedAction == DbUpdateProductAction.ENC ||
                                        selectedAction == DbUpdateProductAction.Both)
                            ? DbUpdateTaskStageStatus.Open.ToString()
                            : DbUpdateTaskStageStatus.Inactive.ToString();
                        stage.Assigned = task.TaskRole.Verifier;
                        break;
                    case DbUpdateTaskStageType.SNC:
                        stage.Status = (selectedAction == DbUpdateProductAction.SNC || selectedAction == DbUpdateProductAction.Both)
                            ? DbUpdateTaskStageStatus.Open.ToString()
                            : DbUpdateTaskStageStatus.Inactive.ToString();
                        stage.Assigned = task.TaskRole.Verifier;
                        break;
                    case DbUpdateTaskStageType.Awaiting_Publication:
                        stage.Status = (selectedAction == DbUpdateProductAction.None)
                            ? DbUpdateTaskStageStatus.Inactive.ToString()
                            : DbUpdateTaskStageStatus.Open.ToString();
                        stage.Assigned = task.TaskRole.Verifier;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


        }

        private void UpdateCarisProjectUsers(TaskInfo task, TaskRole role, int projectId)
        {


            var hpdUser = GetHpdUser(role.Compiler).Result;

            if (role.Compiler != null && role.Compiler != task.TaskRole.Compiler)
                _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);

            if (role.Verifier != null && role.Verifier != task.TaskRole.Verifier)
            {
                hpdUser = GetHpdUser(role.Verifier).Result;
                _carisProjectHelper.UpdateCarisProject(projectId, hpdUser.HpdUsername,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);
            }

        }

        private void UpdateRoles(TaskInfo task, TaskRole role)
        {

            _logger.LogInformation("Updating Roles for for task {ProcessId}.");


            task.TaskRole = role;

            foreach (var taskStage in task.TaskStage.Where
                (s => s.Status != DbUpdateTaskStageStatus.Completed.ToString()))
            {
                taskStage.Assigned = (DbUpdateTaskStageType)taskStage.TaskStageTypeId switch
                {
                    DbUpdateTaskStageType.Compile => role.Compiler,
                    DbUpdateTaskStageType.Verification_Rework => role.Compiler,
                    _ => role.Verifier
                };
            }
        }

        private void UpdateTaskUser(TaskInfo task, TaskRole role)
        {

            _logger.LogInformation("Updating Task stage users from roles for task {ProcessId}.");

            var taskInProgress = task.TaskStage.Find(t => t.Status == DbUpdateTaskStageStatus.InProgress.ToString());

            if (taskInProgress == null)
                task.Assigned = role.Verifier;
            else
            {
                task.Assigned = (DbUpdateTaskStageType)taskInProgress.TaskStageTypeId switch
                {
                    DbUpdateTaskStageType.Compile => role.Compiler,
                    DbUpdateTaskStageType.Verification_Rework => role.Compiler,
                    _ => role.Verifier
                };
            }

        }


        public async Task<JsonResult> OnGetUsersAsync()
        {
            LogContext.PushProperty("ActivityName", "Workflow");
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnGetUsersAsync));
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
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostGetChartDetails));
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




