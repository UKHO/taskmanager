using DbUpdatePortal.Auth;
using DbUpdatePortal.Enums;
using DbUpdatePortal.Helpers;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
        [DisplayName("Product Action Requirement")] public string ProductAction { get; set; }

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



                TargetDate = null;

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

                if (!(_pageValidationHelper.ValidateNewTaskPage(role, TaskName, ChartingArea, UpdateType, ProductAction, ValidationErrorMessages))
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

                if (!Enum.TryParse(this.ProductAction, true, out DbUpdateProductAction selectedAction))
                {
                    selectedAction = DbUpdateProductAction.Both;
                }

                //Assign the status of the task stage 
                taskStage.Status = (DbUpdateTaskStageType)taskStageType.TaskStageTypeId switch
                {
                    DbUpdateTaskStageType.Compile => DbUpdateTaskStageStatus.InProgress.ToString(),
                    DbUpdateTaskStageType.Verify => DbUpdateTaskStageStatus.Open.ToString(),
                    DbUpdateTaskStageType.Verification_Rework => DbUpdateTaskStageStatus.Open.ToString(),
                    DbUpdateTaskStageType.ENC =>
                    selectedAction == DbUpdateProductAction.ENC || selectedAction == DbUpdateProductAction.Both
                        ? DbUpdateTaskStageStatus.Open.ToString()
                        : DbUpdateTaskStageStatus.Inactive.ToString(),
                    DbUpdateTaskStageType.SNC =>
                    selectedAction == DbUpdateProductAction.SNC && selectedAction == DbUpdateProductAction.Both
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

            }

            await _dbUpdateWorkflowDbContext.SaveChangesAsync();

            _logger.LogInformation($"Task Stages created for process Id : {processId}");

            return;

        }
        private async Task PopulateDropDowns()
        {
            var chartingAreas = await _dbUpdateWorkflowDbContext.ChartingArea.OrderBy(i => i.ChartingAreaId).Select(st => st.Name)
                .ToListAsync().ConfigureAwait(false);

            ChartingAreas = new SelectList(chartingAreas);

            var updateTypes = await _dbUpdateWorkflowDbContext.UpdateType.OrderBy(i => i.UpdateTypeId).Select(st => st.Name)
                .ToListAsync().ConfigureAwait(false);

            UpdateTypes = new SelectList(updateTypes);

            var productActions = await _dbUpdateWorkflowDbContext.ProductAction.OrderBy(i => i.ProductActionId).Select(st => st.Name)
                .ToListAsync().ConfigureAwait(false);

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
