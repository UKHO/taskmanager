using DbUpdatePortal.Auth;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DbUpdatePortal.Pages
{
    //[Authorize]
    public class NewTaskModel : PageModel
    {

        private readonly IDbUpdateUserDbService _dbUpdateUserDbService;
        //private readonly IPageValidationHelper _pageValidationHelper;
        //private readonly IStageTypeFactory _stageTypeFactory;
        private readonly DbUpdateWorkflowDbContext _dbUpdateWorkflowDbContext;
        //private readonly IMilestoneCalculator _milestoneCalculator;
        private readonly ILogger<NewTaskModel> _logger;

        [BindProperty]
        [DisplayName("Task Name")] public string TaskName { get; set; }

        [BindProperty]
        [DisplayName("Update Type")] public string UpdateType { get; set; }

        public SelectList UpdateTypes { get; set; }

        [BindProperty]
        [DisplayName("Charting Area")] public string ChartingArea { get; set; }

        public SelectList ChartingAreas { get; set; }


        [BindProperty]
        [DisplayName("Target Date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? TargetDate { get; set; }


        [BindProperty]
        [DisplayName("Compiler")]
        public string Compiler { get; set; }


        [BindProperty]
        [DisplayName("Verifier")]
        public string Verifier { get; set; }



        public List<string> ValidationErrorMessages { get; set; }

        public NewTaskModel(DbUpdateWorkflowDbContext dbUpdateWorkflowDbContext,
                            //IMilestoneCalculator milestoneCalculator,
                            ILogger<NewTaskModel> logger,
                            IDbUpdateUserDbService dbUpdateUserDbService
                            //, IStageTypeFactory stageTypeFactory,
                            //IPageValidationHelper pageValidationHelper
                            )
        {
            _dbUpdateUserDbService = dbUpdateUserDbService;

            try
            {
                _dbUpdateWorkflowDbContext = dbUpdateWorkflowDbContext;
                //_milestoneCalculator = milestoneCalculator;
                _logger = logger;
                //_stageTypeFactory = stageTypeFactory;
                //_pageValidationHelper = pageValidationHelper;

                LogContext.PushProperty("dbUpdatePortalResource", "NewTask");

                TaskName = "";

                SetChartingAreas();
                SetUpdateTypes();

                TargetDate = null;

                ValidationErrorMessages = new List<string>();

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


        //public async Task<IActionResult> OnPostSaveAsync()
        //{
        //    LogContext.PushProperty("ActivityName", "NewTask");
        //    LogContext.PushProperty("NCNEPortalResource", nameof(OnPostSaveAsync));
        //    LogContext.PushProperty("Action", "Save");

        //    try
        //    {

        //        ValidationErrorMessages.Clear();


        //        var role = new TaskRole()
        //        {
        //            Compiler = Compiler,
        //            VerifierOne = Verifier1,
        //            VerifierTwo = Verifier2,
        //            HundredPercentCheck = HundredPercentCheck
        //        };

        //        if (!(_pageValidationHelper.ValidateNewTaskPage(role, WorkflowType, ChartType, ValidationErrorMessages))
        //        )
        //        {

        //            return new JsonResult(this.ValidationErrorMessages)
        //            {
        //                StatusCode = (int)HttpStatusCode.InternalServerError
        //            };
        //        }

        //        ReCalculateDeadlineDates();

        //        var currentStageId = (int)(ChartType == NcneChartType.Adoption.ToString()
        //            ? NcneTaskStageType.With_SDRA
        //            : NcneTaskStageType.Specification);

        //        var currentStage = _ncneWorkflowDbContext.TaskStageType.Find(currentStageId).Name;

        //        var newProcessId = await CreateTaskInfo(currentStage);

        //        await CreateTaskStages(newProcessId);

        //        return StatusCode(200);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error saving Task Information");
        //        return RedirectToPage("./Index");
        //    }

        //}

        //private async Task<int> CreateTaskInfo(string currentStage)
        //{
        //    var taskInfo = _ncneWorkflowDbContext.TaskInfo.Add(entity: new TaskInfo()
        //    {
        //        Ion = this.Ion,
        //        ChartNumber = this.ChartNo,
        //        ChartType = this.ChartType,
        //        WorkflowType = this.WorkflowType,
        //        Duration = Enum.GetName(typeof(DeadlineEnum), Dating),
        //        RepromatDate = this.RepromatDate,
        //        PublicationDate = this.PublicationDate,
        //        AnnounceDate = this.AnnounceDate,
        //        CommitDate = this.CommitToPrintDate,
        //        CisDate = this.CISDate,
        //        Country = this.Country,
        //        AssignedUser = this.Compiler,
        //        AssignedDate = DateTime.Now,
        //        CurrentStage = currentStage,
        //        Status = NcneTaskStatus.InProgress.ToString(),
        //        StatusChangeDate = DateTime.Now,
        //        TaskRole = new TaskRole
        //        {
        //            Compiler = this.Compiler,
        //            VerifierOne = this.Verifier1,
        //            VerifierTwo = this.Verifier2,
        //            HundredPercentCheck = this.HundredPercentCheck
        //        }

        //    });

        //    await _ncneWorkflowDbContext.SaveChangesAsync();
        //    _logger.LogInformation($"New Task created : {taskInfo.Entity.ProcessId}");

        //    return (taskInfo.Entity.ProcessId);
        //}
        //private void ReCalculateDeadlineDates()
        //{
        //    if (RepromatDate != null)
        //        PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)RepromatDate);

        //    if ((PublicationDate != null) && Enum.IsDefined(typeof(DeadlineEnum), this.Dating))
        //    {
        //        var (formsDate, cisDate, commitDate) =
        //            _milestoneCalculator.CalculateMilestones((DeadlineEnum)this.Dating,
        //                (DateTime)this.PublicationDate);

        //        this.CommitToPrintDate = commitDate;
        //        this.CISDate = cisDate;
        //        this.AnnounceDate = formsDate;

        //    }
        //}

        //private async Task CreateTaskStages(int processId)
        //{


        //    foreach (var taskStageType in _stageTypeFactory.GetTaskStages(ChartType))
        //    {
        //        var taskStage = _ncneWorkflowDbContext.TaskStage.Add(new TaskStage()).Entity;

        //        taskStage.ProcessId = processId;
        //        taskStage.TaskStageTypeId = taskStageType.TaskStageTypeId;

        //        //Assign the status of the task stage 
        //        taskStage.Status = (NcneTaskStageType)taskStageType.TaskStageTypeId switch
        //        {
        //            NcneTaskStageType.With_SDRA => NcneTaskStageStatus.InProgress.ToString(),
        //            NcneTaskStageType.Specification => (this.ChartType == NcneChartType.Adoption.ToString()
        //                       ? NcneTaskStageStatus.Open.ToString() :
        //                       NcneTaskStageStatus.InProgress.ToString()),
        //            NcneTaskStageType.V2 => (this.Verifier2 == null
        //                ? NcneTaskStageStatus.Inactive.ToString()
        //                : NcneTaskStageStatus.Open.ToString()),
        //            NcneTaskStageType.V2_Rework => (this.Verifier2 == null
        //                ? NcneTaskStageStatus.Inactive.ToString()
        //                : NcneTaskStageStatus.Open.ToString()),
        //            NcneTaskStageType.Forms => NcneTaskStageStatus.InProgress.ToString(),
        //            _ => NcneTaskStageStatus.Open.ToString()
        //        };

        //        //Assign the user according to the stage
        //        taskStage.AssignedUser = (NcneTaskStageType)taskStageType.TaskStageTypeId switch
        //        {
        //            NcneTaskStageType.With_Geodesy => this.Compiler,
        //            NcneTaskStageType.With_SDRA => this.Compiler,
        //            NcneTaskStageType.Specification => this.Compiler,
        //            NcneTaskStageType.Compile => this.Compiler,
        //            NcneTaskStageType.V1_Rework => this.Compiler,
        //            NcneTaskStageType.V2_Rework => this.Compiler,
        //            NcneTaskStageType.V2 => this.Verifier2,
        //            NcneTaskStageType.Hundred_Percent_Check => HundredPercentCheck,
        //            _ => this.Verifier1
        //        };


        //        //set the Expected Date of completion for Forms, Commit to print , CIS and publication
        //        taskStage.DateExpected = (NcneTaskStageType)taskStageType.TaskStageTypeId switch
        //        {
        //            NcneTaskStageType.Forms => this.AnnounceDate,
        //            NcneTaskStageType.Commit_To_Print => this.CommitToPrintDate,
        //            NcneTaskStageType.CIS => this.CISDate,
        //            NcneTaskStageType.Publication => this.PublicationDate,
        //            _ => null
        //        };

        //    }

        //    await _ncneWorkflowDbContext.SaveChangesAsync();

        //    _logger.LogInformation($"Task Stages created for process Id : {processId}");

        //    return;

        //}
        private void SetChartingAreas()
        {
            if (!System.IO.File.Exists(@"Data\ChartingAreas.json"))
                throw new FileNotFoundException(@"Data\ChartingAreas.json");


            var jsonString = System.IO.File.ReadAllText(@"Data\ChartingAreas.json");

            var chartingAreas = JsonConvert.DeserializeObject<IEnumerable<ChartingArea>>(jsonString).Select(sc => sc.Name);

            ChartingAreas = new SelectList(chartingAreas);
        }

        private void SetUpdateTypes()
        {
            if (!System.IO.File.Exists(@"Data\UpdateTypes.json"))
                throw new FileNotFoundException(@"Data\UpdateTypes.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\UpdateTypes.json");

            var updateTypes = JsonConvert.DeserializeObject<IEnumerable<UpdateType>>(jsonString)
                .Select(u => u.Name);

            UpdateTypes = new SelectList(updateTypes);
        }

        //public JsonResult OnPostCalcPublishDate(DateTime dtRepromat)
        //{

        //    PublicationDate = _milestoneCalculator.CalculatePublishDate((DateTime)dtRepromat);

        //    return new JsonResult(PublicationDate?.ToShortDateString());
        //}

        public async Task<JsonResult> OnGetUsersAsync()
        {

            var users =
                (await _dbUpdateUserDbService.GetUsersFromDbAsync()).Select(u => new
                {
                    u.DisplayName,
                    u.UserPrincipalName
                });

            return new JsonResult(users);

        }

    }
}
