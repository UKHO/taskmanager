using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
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
using System.Net;
using System.Threading.Tasks;

namespace NCNEPortal
{
    [Authorize]
    public class NewTaskModel : PageModel
    {
        private readonly INcneUserDbService _ncneUserDbService;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly IStageTypeFactory _stageTypeFactory;
        private readonly NcneWorkflowDbContext _ncneWorkflowDbContext;
        private readonly IMilestoneCalculator _milestoneCalculator;
        private readonly ILogger<NewTaskModel> _logger;

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
        public AdUser Compiler { get; set; }


        [BindProperty]
        [DisplayName("Verifier V1")]
        public AdUser Verifier1 { get; set; }


        [BindProperty]
        [DisplayName("Verifier V2")]
        public AdUser Verifier2 { get; set; }


        [BindProperty]
        [DisplayName("100% Check")]
        public AdUser HundredPercentCheck { get; set; }


        public List<string> ValidationErrorMessages { get; set; }

        public NewTaskModel(NcneWorkflowDbContext ncneWorkflowDbContext,
                            IMilestoneCalculator milestoneCalculator,
                            ILogger<NewTaskModel> logger,
                            INcneUserDbService ncneUserDbService,
                            IStageTypeFactory stageTypeFactory,
                            IPageValidationHelper pageValidationHelper)
        {
            _ncneUserDbService = ncneUserDbService;

            try
            {
                _ncneWorkflowDbContext = ncneWorkflowDbContext;
                _milestoneCalculator = milestoneCalculator;
                _logger = logger;
                _stageTypeFactory = stageTypeFactory;
                _pageValidationHelper = pageValidationHelper;

                LogContext.PushProperty("NCNEPortalResource", "NewTask");

                Ion = "";
                ChartNo = "";
                Country = "";

                PublicationDate = null;

                ValidationErrorMessages = new List<string>();

            }
            catch (Exception ex)
            {

                ValidationErrorMessages.Add(ex.Message);
                _logger.LogError(ex, "Error while initializing the new task page");

            }
        }

        public async Task OnGetAsync()
        {
            await PopulateDropDowns();
        }


        public async Task<IActionResult> OnPostSaveAsync()
        {
            LogContext.PushProperty("ActivityName", "NewTask");
            LogContext.PushProperty("NCNEPortalResource", nameof(OnPostSaveAsync));
            LogContext.PushProperty("Action", "Save");

            try
            {

                ValidationErrorMessages.Clear();

                var role = new TaskRole()
                {
                    Compiler = string.IsNullOrEmpty(Compiler?.UserPrincipalName) ? null : await _ncneUserDbService.GetAdUserAsync(Compiler.UserPrincipalName),
                    VerifierOne = string.IsNullOrEmpty(Verifier1?.UserPrincipalName) ? null : await _ncneUserDbService.GetAdUserAsync(Verifier1.UserPrincipalName),
                    VerifierTwo = string.IsNullOrEmpty(Verifier2?.UserPrincipalName) ? null : await _ncneUserDbService.GetAdUserAsync(Verifier2.UserPrincipalName),
                    HundredPercentCheck = string.IsNullOrEmpty(HundredPercentCheck?.UserPrincipalName) ? null : await _ncneUserDbService.GetAdUserAsync(HundredPercentCheck.UserPrincipalName)
                };

                if (!(_pageValidationHelper.ValidateNewTaskPage(role, WorkflowType, ChartType, ValidationErrorMessages))
                )
                {

                    return new JsonResult(this.ValidationErrorMessages)
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }

                ReCalculateDeadlineDates();

                var currentStageId = (int)(ChartType == NcneChartType.Adoption.ToString()
                    ? NcneTaskStageType.With_SDRA
                    : NcneTaskStageType.Specification);

                var currentStage = _ncneWorkflowDbContext.TaskStageType.Find(currentStageId).Name;

                var newProcessId = await CreateTaskInfo(currentStage, role);

                await CreateTaskStages(newProcessId, role);

                return StatusCode(200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Task Information");
                return RedirectToPage("./Index");
            }

        }

        private async Task<int> CreateTaskInfo(string currentStage, TaskRole role)
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
                Assigned = role.Compiler,
                AssignedDate = DateTime.Now,
                CurrentStage = currentStage,
                Status = NcneTaskStatus.InProgress.ToString(),
                StatusChangeDate = DateTime.Now,
                TaskRole = role

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

        private async Task CreateTaskStages(int processId, TaskRole role)
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
                    NcneTaskStageType.V2 => (role.VerifierTwo == null
                        ? NcneTaskStageStatus.Inactive.ToString()
                        : NcneTaskStageStatus.Open.ToString()),
                    NcneTaskStageType.V2_Rework => (role.VerifierTwo == null
                        ? NcneTaskStageStatus.Inactive.ToString()
                        : NcneTaskStageStatus.Open.ToString()),
                    NcneTaskStageType.Forms => NcneTaskStageStatus.InProgress.ToString(),
                    _ => NcneTaskStageStatus.Open.ToString()
                };

                //Assign the user according to the stage
                taskStage.Assigned = (NcneTaskStageType)taskStageType.TaskStageTypeId switch
                {
                    NcneTaskStageType.With_Geodesy => role.Compiler,
                    NcneTaskStageType.With_SDRA => role.Compiler,
                    NcneTaskStageType.Specification => role.Compiler,
                    NcneTaskStageType.Compile => role.Compiler,
                    NcneTaskStageType.V1_Rework => role.Compiler,
                    NcneTaskStageType.V2_Rework => role.Compiler,
                    NcneTaskStageType.V2 => role.VerifierTwo,
                    NcneTaskStageType.Hundred_Percent_Check => role.HundredPercentCheck,
                    _ => role.VerifierOne
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

        private async Task PopulateDropDowns()
        {
            var chartTypes = await _ncneWorkflowDbContext.ChartType.OrderBy(i => i.ChartTypeId)
                .Select(st => st.Name)
                .ToListAsync().ConfigureAwait(false);

            ChartTypes = new SelectList(chartTypes);

            var workflowTypes = await _ncneWorkflowDbContext.WorkflowType.OrderBy(i => i.WorkflowTypeId)
                .Select(st => st.Name)
                .ToListAsync().ConfigureAwait(false);

            WorkflowTypes = new SelectList(workflowTypes);


        }
        public JsonResult OnPostCalcPublishDate(DateTime dtRepromat)
        {

            PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)dtRepromat);

            return new JsonResult(PublicationDate?.ToShortDateString());
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

    }
}
