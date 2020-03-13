using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Enums;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ChartType = NCNEPortal.Models.ChartType;
using WorkflowType = NCNEPortal.Models.WorkflowType;


namespace NCNEPortal
{
    [Authorize]
    public class NewTaskModel : PageModel
    {
        private readonly IStageTypeFactory _stageTypeFactory;
        private readonly IDirectoryService _directoryService;
        private readonly NcneWorkflowDbContext _ncneWorkflowDbContext;
        private readonly IMilestoneCalculator _milestoneCalculator;
        private readonly ILogger<NewTaskModel> _logger;
        private readonly IUserIdentityService _userIdentityService;

        [BindProperty]
        [DisplayName("ION")] public string Ion { get; set; }

        [BindProperty]
        [DisplayName("Chart number")] public string ChartNo { get; set; }

        [BindProperty]
        [DisplayName("Country")] public string Country { get; set; }

        [BindProperty]
        [DisplayName("Chart type")] public string ChartType { get; set; }

        public SelectList ChartTypes { get; set; }

        [BindProperty]
        [DisplayName("Workflow type")] public string WorkflowType { get; set; }

        public SelectList WorkflowTypes { get; set; }

        [BindProperty]
        [DisplayName("Duration")] public int Dating { get; set; }

        [BindProperty]
        [DisplayName("Publication date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? PublicationDate { get; set; }

        [BindProperty]
        [DisplayName("Repromat received date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? RepromatDate { get; set; }

        [DisplayName("H Forms/Announce:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? AnnounceDate { get; set; }

        [DisplayName("Commit to Print:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? CommitToPrintDate { get; set; }

        [DisplayName("CIS:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? CISDate { get; set; }

        [BindProperty]
        [DisplayName("Compiler")]
        public string Compiler { get; set; }


        [BindProperty]
        [DisplayName("Verifier V1")]
        public string Verifier1 { get; set; }


        [BindProperty]
        [DisplayName("Verifier V2")]
        public string Verifier2 { get; set; }


        [BindProperty]
        [DisplayName("Publication")]
        public string Publisher { get; set; }


        public List<string> ValidationErrorMessages { get; set; }

        public List<String> userList = new List<string>();

        public NewTaskModel(NcneWorkflowDbContext ncneWorkflowDbContext,
                            IMilestoneCalculator milestoneCalculator,
                            ILogger<NewTaskModel> logger,
                            IUserIdentityService userIdentityService,
                            IDirectoryService directoryService,
                            IStageTypeFactory stageTypeFactory)
        {

            try
            {
                _directoryService = directoryService;
                _ncneWorkflowDbContext = ncneWorkflowDbContext;
                _milestoneCalculator = milestoneCalculator;
                _logger = logger;
                _userIdentityService = userIdentityService;
                _stageTypeFactory = stageTypeFactory;


                LogContext.PushProperty("NCNEPortalResource", "NewTask");

                Ion = "";
                ChartNo = "";
                Country = "";

                SetChartTypes();
                SetWorkflowTypes();

                PublicationDate = null;

                ValidationErrorMessages = new List<string>();

                userList = _directoryService.GetGroupMembers().Result.ToList();

            }
            catch (Exception ex)
            {

                ValidationErrorMessages.Add(ex.Message);
                _logger.LogError(ex, "Error while initializing the new task page");

            }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {

            LogContext.PushProperty("ActivityName", "NewTask");
            LogContext.PushProperty("NCNEPortalResource", nameof(OnPost));
            LogContext.PushProperty("Action", "Post");

            try
            {

                ReCalculateDeadlineDates();

                var newProcessId = await CreateTaskInfo();

                await CreateTaskStages(newProcessId);


                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Task Information");
                return RedirectToPage("./Index");
            }
        }

        private async Task<int> CreateTaskInfo()
        {
            var taskInfo = _ncneWorkflowDbContext.TaskInfo.Add(entity: new TaskInfo()
            {
                Ion = this.Ion,
                ChartNumber = this.ChartNo,
                ChartType = this.ChartType,
                WorkflowType = this.WorkflowType,
                Duration = Enum.GetName(typeof(DeadlineEnum), Dating),
                RepromatDate = this.RepromatDate,
                PublicationDate = this.PublicationDate,
                AnnounceDate = this.AnnounceDate,
                CommitDate = this.CommitToPrintDate,
                CisDate = this.CISDate,
                Country = this.Country,
                AssignedUser = this.Compiler,
                AssignedDate = DateTime.Now,
                Status = NcneTaskStatus.InProgress.ToString(),
                TaskRole = new TaskRole
                {
                    Compiler = this.Compiler,
                    VerifierOne = this.Verifier1,
                    VerifierTwo = this.Verifier2,
                    Publisher = this.Publisher
                }

            });

            await _ncneWorkflowDbContext.SaveChangesAsync();
            _logger.LogInformation($"New Task created : {taskInfo.Entity.ProcessId}");

            return (taskInfo.Entity.ProcessId);
        }
        private void ReCalculateDeadlineDates()
        {
            if (RepromatDate != null)
                PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)RepromatDate);

            if ((PublicationDate != null) && Enum.IsDefined(typeof(DeadlineEnum), this.Dating))
            {
                var (formsDate, cisDate, commitDate) =
                    _milestoneCalculator.CalculateMilestones((DeadlineEnum)this.Dating,
                        (DateTime)this.PublicationDate);

                this.CommitToPrintDate = commitDate;
                this.CISDate = cisDate;
                this.AnnounceDate = formsDate;

            }
        }

        private async Task CreateTaskStages(int processId)
        {


            foreach (var taskStageType in _stageTypeFactory.GetTaskStages(ChartType))
            {
                var taskStage = _ncneWorkflowDbContext.TaskStage.Add(new TaskStage()).Entity;

                taskStage.ProcessId = processId;
                taskStage.TaskStageTypeId = taskStageType.TaskStageTypeId;

                //Assign the status of the task stage 
                taskStage.Status = (NcneTaskStageType)taskStageType.TaskStageTypeId switch
                {
                    NcneTaskStageType.With_SDRA => NcneTaskStageStatus.InProgress.ToString(),
                    NcneTaskStageType.Specification => (this.ChartType == NcneChartType.Adoption.ToString()
                               ? NcneTaskStageStatus.Open.ToString() :
                               NcneTaskStageStatus.InProgress.ToString()),
                    NcneTaskStageType.V2 => (this.Verifier2 == null
                        ? NcneTaskStageStatus.Inactive.ToString()
                        : NcneTaskStageStatus.Open.ToString()),
                    NcneTaskStageType.V2_Rework => (this.Verifier2 == null
                        ? NcneTaskStageStatus.Inactive.ToString()
                        : NcneTaskStageStatus.Open.ToString()),
                    _ => NcneTaskStageStatus.Open.ToString()
                };

                //Assign the user according to the stage
                taskStage.AssignedUser = (NcneTaskStageType)taskStageType.TaskStageTypeId switch
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


                //set the Expected Date of completion for Forms, Commit to print , CIS and publication
                taskStage.DateExpected = (NcneTaskStageType)taskStageType.TaskStageTypeId switch
                {
                    NcneTaskStageType.Forms => this.AnnounceDate,
                    NcneTaskStageType.Commit_To_Print => this.CommitToPrintDate,
                    NcneTaskStageType.CIS => this.CISDate,
                    NcneTaskStageType.Publication => this.PublicationDate,
                    _ => null
                };

            }

            await _ncneWorkflowDbContext.SaveChangesAsync();

            _logger.LogInformation($"Task Stages created for process Id : {processId}");

            return;

        }
        private void SetChartTypes()
        {
            if (!System.IO.File.Exists(@"Data\ChartTypes.json"))
                throw new FileNotFoundException(@"Data\ChartTypes.json");


            var jsonString = System.IO.File.ReadAllText(@"Data\ChartTypes.json");

            var chartTypes = JsonConvert.DeserializeObject<IEnumerable<ChartType>>(jsonString).Select(sc => sc.Name);

            ChartTypes = new SelectList(chartTypes);
        }

        private void SetWorkflowTypes()
        {
            if (!System.IO.File.Exists(@"Data\WorkflowTypes.json"))
                throw new FileNotFoundException(@"Data\WorkflowTypes.json");


            var jsonString = System.IO.File.ReadAllText(@"Data\WorkflowTypes.json");

            var workflowTypes = JsonConvert.DeserializeObject<IEnumerable<WorkflowType>>(jsonString)
                .Select(sc => sc.Name);

            WorkflowTypes = new SelectList(workflowTypes);
        }

        public async Task<IActionResult> OnPostAssignRoleToUserAsync(string compiler, string verifierOne, string verifierTwo, string publisher)
        {

            LogContext.PushProperty("NCNEPortalResource", nameof(OnPostAssignRoleToUserAsync));

            ValidationErrorMessages.Clear();

            if (string.IsNullOrEmpty(compiler))
                ValidationErrorMessages.Add("Please assign valid user to the Compiler role to create a new task");
            else

            {
                if (!userList.Any(a => a == compiler))
                {
                    _logger.LogInformation($"Attempted to assign Compiler role to unknown user {compiler}");
                    ValidationErrorMessages.Add($"Unable to assign Compiler role to unknown user {compiler}");
                }
            }


            if (!string.IsNullOrEmpty(verifierOne))
            {
                if (!userList.Any(a => a == verifierOne))
                {
                    _logger.LogInformation($"Attempted to assign Verifier1 role to unknown user {verifierOne}");
                    ValidationErrorMessages.Add($"Unable to assign Verifier1 role to unknown user {verifierOne}");
                }
            }

            if (!string.IsNullOrEmpty(verifierTwo))
            {
                if (!userList.Any(a => a == verifierTwo))
                {
                    _logger.LogInformation($"Attempted to assign Verifier2 role to unknown user {verifierTwo}");
                    ValidationErrorMessages.Add($"Unable to assign Verifier2 role to unknown user {verifierTwo}");
                }
            }

            if (!string.IsNullOrEmpty(publisher))
            {
                if (!userList.Any(a => a == publisher))
                {
                    _logger.LogInformation($"Attempted to assign Publisher role to unknown user {publisher}");
                    ValidationErrorMessages.Add($"Unable to assign Publisher role to unknown user {publisher}");
                }
            }

            if (ValidationErrorMessages.Any())
            {
                return new JsonResult(this.ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }


            return StatusCode(200);
        }


        public async Task<JsonResult> OnPostCalcPublishDateAsync(DateTime dtRepromat)
        {

            PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)dtRepromat);

            return new JsonResult(PublicationDate?.ToShortDateString());
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

    }
}
