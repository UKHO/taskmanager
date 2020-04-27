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

        public CarisProjectDetails CarisProjectDetails { get; set; }
        public bool IsCarisProjectCreated { get; set; }

        public List<string> ValidationErrorMessages { get; set; }
        public List<string> userList = new List<string>();

        private string _userFullName;

        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
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
            userList = _ncneUserDbService.GetUsersFromDbAsync().Result.Select(u => u.DisplayName).ToList();
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

            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

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

        public void OnGetAsync(int processId)
        {
            ProcessId = processId;

            var taskInfo = _dbContext.TaskInfo
                .Include(task => task.TaskRole)
                .Include(task => task.TaskStage).ThenInclude(comment => comment.TaskStageComment)
                .Include(task => task.TaskStage).ThenInclude(stagetype => stagetype.TaskStageType)
                .Include(task => task.TaskComment)
                .FirstOrDefault(t => t.ProcessId == processId);


            Ion = taskInfo.Ion;
            ChartTitle = taskInfo.ChartTitle;
            ChartNo = taskInfo.ChartNumber;
            WorkflowType = taskInfo.WorkflowType;
            ChartType = taskInfo.ChartType;
            Country = taskInfo.Country;

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

            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

            LogContext.PushProperty("UserFullName", UserFullName);

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
            var hpdUser = await GetHpdUser(UserFullName);

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
                CreatedBy = UserFullName,
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
                _logger.LogError("Unable to find HPD Username for {UserFullName} in our system.");
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

        public async Task<IActionResult> OnPostValidateCompleteAsync(int processId, int stageId, string username)
        {
            ValidationErrorMessages.Clear();
            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

            if (!(_pageValidationHelper.ValidateForCompletion(username, UserFullName, ValidationErrorMessages)))
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            return new JsonResult(HttpStatusCode.OK);
        }

        public async Task<IActionResult> OnPostCompleteAsync(int processId, int stageId, string username)
        {
            var result = await CompleteStage(processId, stageId);

            return new JsonResult(HttpStatusCode.OK);
        }

        private async Task<bool> CompleteStage(int processId, int stageId)
        {


            var taskStages = _dbContext.TaskStage.Where(s => s.ProcessId == processId);

            var currentStage = await taskStages.SingleAsync(t => t.TaskStageId == stageId);

            currentStage.Status = NcneTaskStageStatus.Completed.ToString();
            currentStage.DateCompleted = DateTime.Now;


            var nextStages = _workflowStageHelper.GetNextStagesForCompletion((NcneTaskStageType) currentStage.TaskStageTypeId);

            if (nextStages.Count > 0)
            {
                foreach (var stage in nextStages)
                {
                    taskStages.First(t => t.TaskStageTypeId == (int)stage).Status =
                        NcneTaskStageStatus.InProgress.ToString();
                }
            }

            await _dbContext.SaveChangesAsync();

            return true;
        }

        private List<int> GetNextStage(int stageTypeId)
        {
            List<int> result = new List<int>();

            switch ((NcneTaskStageType)stageTypeId)
            {
                case NcneTaskStageType.With_SDRA:
                    result.Add((int)NcneTaskStageType.With_Geodesy);
                    break;
                case NcneTaskStageType.With_Geodesy:
                    result.Add((int)NcneTaskStageType.Specification);
                    break;
                case NcneTaskStageType.Specification:
                    result.Add((int)NcneTaskStageType.Compile);
                    break;
                case NcneTaskStageType.Compile:
                    result.Add((int)NcneTaskStageType.V1);
                    break;
                case NcneTaskStageType.V1:
                    result.Add((int)NcneTaskStageType.V2);
                    break;
                case NcneTaskStageType.V1_Rework:
                    result.Add((int)NcneTaskStageType.V1);
                    break;
                case NcneTaskStageType.V2:
                    result.Add((int)NcneTaskStageType.Final_Updating);
                    break;
                case NcneTaskStageType.V2_Rework:
                    result.Add((int)NcneTaskStageType.V2);
                    break;
                case NcneTaskStageType.Forms:
                    break;
                case NcneTaskStageType.Final_Updating:
                    result.Add((int)NcneTaskStageType.Hundred_Percent_Check);
                    break;
                case NcneTaskStageType.Hundred_Percent_Check:
                    result.Add((int)NcneTaskStageType.Commit_To_Print);
                    break;
                case NcneTaskStageType.Commit_To_Print:
                    result.Add((int)NcneTaskStageType.CIS);
                    break;
                case NcneTaskStageType.CIS:
                    result.Add((int)NcneTaskStageType.Publication);
                    result.Add((int)NcneTaskStageType.Publish_Chart);
                    result.Add((int)NcneTaskStageType.Clear_Vector);
                    result.Add((int)NcneTaskStageType.Retire_Old_Version);
                    result.Add((int)NcneTaskStageType.Consider_Withdrawn_Charts);
                    break;
                case NcneTaskStageType.Publication:
                    break;
                case NcneTaskStageType.Publish_Chart:
                    break;
                case NcneTaskStageType.Clear_Vector:
                    break;
                case NcneTaskStageType.Retire_Old_Version:
                    break;
                case NcneTaskStageType.Consider_Withdrawn_Charts:
                    break;
            }

            return result;
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
            task.RepromatDate = RepromatDate;
            if (chartType == "Adoption" && RepromatDate != null)
            {
                task.PublicationDate =
                    PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)RepromatDate);

            }
            else
            {
                task.PublicationDate = PublicationDate;
            }


            task.AnnounceDate = AnnounceDate;
            task.CommitDate = CommitToPrintDate;
            task.CisDate = CISDate;
            task.Country = Country;
            task.TaskRole.Compiler = Compiler;
            task.TaskRole.VerifierOne = Verifier1;
            task.TaskRole.VerifierTwo = Verifier2;
            task.TaskRole.Publisher = Publisher;

            task.ThreePs = SentTo3Ps;
            task.SentDate3Ps = SendDate3ps;
            task.ExpectedDate3Ps = ExpectedReturnDate3ps;
            task.ActualDate3Ps = ActualReturnDate3ps;

            int formStageId = 0;
            int cisStageId = 0;
            int commitStageId = 0;
            int publishStageId = 0;

            //Update deadline dates in the taskstages
            foreach (var taskStage in task.TaskStage)
            {
                switch ((NcneTaskStageType)taskStage.TaskStageTypeId)
                {
                    case NcneTaskStageType.Forms:
                        {
                            taskStage.DateExpected = AnnounceDate;
                            formStageId = taskStage.TaskStageId;
                            break;
                        }

                    case NcneTaskStageType.Commit_To_Print:
                        {
                            taskStage.DateExpected = CommitToPrintDate;
                            commitStageId = taskStage.TaskStageId;
                            break;
                        }

                    case NcneTaskStageType.CIS:
                        {
                            taskStage.DateExpected = CISDate;
                            cisStageId = taskStage.TaskStageId;
                            break;
                        }

                    case NcneTaskStageType.Publication:
                        {
                            taskStage.DateExpected = PublicationDate;
                            publishStageId = taskStage.TaskStageId;
                            break;
                        }
                }
            }

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

            await _dbContext.SaveChangesAsync();

            return new DeadlineId()
            {
                FormsDate = formStageId,
                CommitDate = commitStageId,
                CisDate = cisStageId,
                PublishDate = publishStageId
            };


        }



        public async Task<JsonResult> OnGetUsersAsync()
        {
            LogContext.PushProperty("NCNEPortalResource", nameof(OnGetUsersAsync));
            LogContext.PushProperty("Action", "GetUsersForTypeAhead");

            try
            {
                if (userList.Count == 0)
                {
                    return new JsonResult("Error")
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }

                return new JsonResult(userList);

            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Unable to get the user list");
                return new JsonResult(ex.Message)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

        }

        public async Task<JsonResult> OnPostTaskCommentAsync(string txtComment, int commentProcessId)
        {
            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

            txtComment = string.IsNullOrEmpty(txtComment) ? string.Empty : txtComment.Trim();

            if (!string.IsNullOrEmpty(txtComment))
            {
                await _dbContext.TaskComment.AddAsync(new TaskComment()
                {
                    ProcessId = commentProcessId,
                    ActionIndicator = false,
                    Comment = txtComment,
                    Created = DateTime.Now,
                    Username = UserFullName
                });

                await _dbContext.SaveChangesAsync();
            }

            var result = new[]
            {
                UserFullName,
                DateTime.Now.ToLongDateString()
            };
            return new JsonResult(result);
        }

        public async Task<JsonResult> OnPostStageCommentAsync(string txtComment, int commentProcessId, int stageId)
        {
            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);

            txtComment = string.IsNullOrEmpty(txtComment) ? string.Empty : txtComment.Trim();


            if (!string.IsNullOrEmpty(txtComment))
            {
                await _dbContext.TaskStageComment.AddAsync(new TaskStageComment()
                {
                    ProcessId = commentProcessId,
                    TaskStageId = stageId,
                    Comment = txtComment,
                    Created = DateTime.Now,
                    Username = UserFullName
                });

                await _dbContext.SaveChangesAsync();
            }

            var result = new[]
            {
                UserFullName,
                DateTime.Now.ToLongDateString()
            };
            return new JsonResult(result);

        }

    }
}




