using DbUpdatePortal.Auth;
using DbUpdatePortal.Enums;
using DbUpdatePortal.Helpers;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.AspNetCore.Authorization;
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
using System.Net;
using System.Threading.Tasks;

namespace DbUpdatePortal.Pages
{
    [Authorize]
    public class NewTaskModel : PageModel
    {

        private readonly IDbUpdateUserDbService _dbUpdateUserDbService;
        private readonly IPageValidationHelper _pageValidationHelper;
        private readonly IStageTypeFactory _stageTypeFactory;
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
        [DisplayName("Product Action")] public string ProductAction { get; set; }

        public SelectList ProductActions { get; set; }

        [BindProperty]
        [DisplayName("Target Date")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime? TargetDate { get; set; }


        [BindProperty]
        [DisplayName("Compiler")]
        public AdUser Compiler { get; set; }


        [BindProperty]
        [DisplayName("Verifier")]
        public AdUser Verifier { get; set; }



        public List<string> ValidationErrorMessages { get; set; }

        public NewTaskModel(DbUpdateWorkflowDbContext dbUpdateWorkflowDbContext,
                            //IMilestoneCalculator milestoneCalculator,
                            ILogger<NewTaskModel> logger,
                            IDbUpdateUserDbService dbUpdateUserDbService
                            , IStageTypeFactory stageTypeFactory,
                             IPageValidationHelper pageValidationHelper
                            )
        {
            _dbUpdateUserDbService = dbUpdateUserDbService;

            try
            {
                _dbUpdateWorkflowDbContext = dbUpdateWorkflowDbContext;
                //_milestoneCalculator = milestoneCalculator;
                _logger = logger;
                _stageTypeFactory = stageTypeFactory;
                _pageValidationHelper = pageValidationHelper;

                LogContext.PushProperty("dbUpdatePortalResource", "NewTask");

                TaskName = "";

                SetChartingAreas();
                SetUpdateTypes();
                SetProductActions();

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


        public async Task<IActionResult> OnPostSaveAsync()
        {
            LogContext.PushProperty("ActivityName", "NewTask");
            LogContext.PushProperty("DbUpdatePortalResource", nameof(OnPostSaveAsync));
            LogContext.PushProperty("Action", "Save");

            try
            {

                ValidationErrorMessages.Clear();


                var role = new TaskRole()
                {
                    Compiler = string.IsNullOrEmpty(Compiler?.UserPrincipalName) ? null : await _dbUpdateUserDbService.GetAdUserAsync(Compiler.UserPrincipalName),
                    Verifier = string.IsNullOrEmpty(Verifier?.UserPrincipalName) ? null : await _dbUpdateUserDbService.GetAdUserAsync(Verifier.UserPrincipalName),
                };

                if (!(_pageValidationHelper.ValidateNewTaskPage(role, ChartingArea, UpdateType, ProductAction, ValidationErrorMessages))
                )
                {

                    return new JsonResult(this.ValidationErrorMessages)
                    {
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                }


                var currentStage = DbUpdateTaskStageType.Compile.ToString();

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
            try
            {



                var taskInfo = _dbUpdateWorkflowDbContext.TaskInfo.Add(entity: new TaskInfo()
                {
                    Name = this.TaskName,
                    ChartingArea = this.ChartingArea,
                    UpdateType = this.UpdateType,
                    ProductAction = this.ProductAction,
                    TargetDate = this.TargetDate,
                    Assigned = role.Compiler,
                    AssignedDate = DateTime.Now,
                    CurrentStage = currentStage,
                    Status = DbUpdateTaskStatus.InProgress.ToString(),
                    StatusChangeDate = DateTime.Now,
                    TaskRole = role

                });

                await _dbUpdateWorkflowDbContext.SaveChangesAsync();
                _logger.LogInformation($"New Task created : {taskInfo.Entity.ProcessId}");

                return (taskInfo.Entity.ProcessId);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Task Information");
                return 0;
            }
        }

        private async Task CreateTaskStages(int processId, TaskRole role)
        {


            foreach (var taskStageType in _stageTypeFactory.GetTaskStages(ProductAction))
            {
                var taskStage = _dbUpdateWorkflowDbContext.TaskStage.Add(new TaskStage()).Entity;

                taskStage.ProcessId = processId;
                taskStage.TaskStageTypeId = taskStageType.TaskStageTypeId;
                Enum.TryParse(this.ProductAction, out DbUpdateProductAction action);

                //Assign the status of the task stage 
                taskStage.Status = (DbUpdateTaskStageType)taskStageType.TaskStageTypeId switch
                {
                    DbUpdateTaskStageType.Compile => DbUpdateTaskStageStatus.InProgress.ToString(),
                    DbUpdateTaskStageType.Verify => DbUpdateTaskStageStatus.Open.ToString(),
                    DbUpdateTaskStageType.Verification_Rework => DbUpdateTaskStageStatus.Open.ToString(),
                    DbUpdateTaskStageType.ENC =>
                    action != DbUpdateProductAction.None && action != DbUpdateProductAction.SNC
                        ? DbUpdateTaskStageStatus.Open.ToString()
                        : DbUpdateTaskStageStatus.Inactive.ToString(),
                    DbUpdateTaskStageType.SNC =>
                    action != DbUpdateProductAction.None && action != DbUpdateProductAction.ENC
                        ? DbUpdateTaskStageStatus.Open.ToString()
                        : DbUpdateTaskStageStatus.Inactive.ToString(),
                    _ => DbUpdateTaskStageStatus.Inactive.ToString()
                };

                //Assign the user according to the stage
                taskStage.Assigned = (DbUpdateTaskStageType)taskStageType.TaskStageTypeId switch
                {
                    DbUpdateTaskStageType.Compile => role.Compiler,
                    DbUpdateTaskStageType.Verification_Rework => role.Compiler,

                    _ => role.Verifier
                };


                ////set the Expected Date of completion for Forms, Commit to print , CIS and publication
                //taskStage.DateExpected = (NcneTaskStageType)taskStageType.TaskStageTypeId switch
                //{
                //    NcneTaskStageType.Forms => this.AnnounceDate,
                //    NcneTaskStageType.Commit_To_Print => this.CommitToPrintDate,
                //    NcneTaskStageType.CIS => this.CISDate,
                //    NcneTaskStageType.Publication => this.PublicationDate,
                //    _ => null
                //};

            }

            await _dbUpdateWorkflowDbContext.SaveChangesAsync();

            _logger.LogInformation($"Task Stages created for process Id : {processId}");

            return;

        }
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

        private void SetProductActions()
        {
            if (!System.IO.File.Exists(@"Data\ProductActions.json"))
                throw new FileNotFoundException(@"Data\ProductActions.json");

            var jsonString = System.IO.File.ReadAllText(@"Data\ProductActions.json");

            var productActions = JsonConvert.DeserializeObject<IEnumerable<ProductAction>>(jsonString)
                .Select(u => u.Name);

            ProductActions = new SelectList(productActions);
        }

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
