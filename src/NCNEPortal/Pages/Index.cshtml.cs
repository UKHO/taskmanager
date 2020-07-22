using Common.Helpers;
using Common.Helpers.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCNEPortal.Auth;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace NCNEPortal.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly INcneUserDbService _ncneUserDbService;
        private readonly NcneWorkflowDbContext _dbContext;
        private readonly ILogger<IndexModel> _logger;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;

        private (string DisplayName, string UserPrincipalName) _currentUser;
        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        [BindProperty(SupportsGet = true)]
        public List<TaskInfo> NcneTasks { get; set; }

        public List<string> ValidationErrorMessages { get; set; }
        public IndexModel(INcneUserDbService ncneUserDbService,
                          NcneWorkflowDbContext ncneWorkflowDbContext,
                          ILogger<IndexModel> logger,
                          IAdDirectoryService adDirectoryService,
                          ICarisProjectHelper carisProjectHelper,
                          IOptions<GeneralConfig> generalConfig)
        {
            _ncneUserDbService = ncneUserDbService;
            _dbContext = ncneWorkflowDbContext;
            _logger = logger;
            _adDirectoryService = adDirectoryService;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;

            ValidationErrorMessages = new List<string>();
        }

        public async Task OnGetAsync()
        {
            NcneTasks = await _dbContext.TaskInfo
                .Include(t => t.Assigned)
                .Include(c => c.TaskNote)
                .ThenInclude(t => t.CreatedBy)
                .Include(c => c.TaskNote)
                .ThenInclude(t => t.LastModifiedBy)
                .Include(s => s.TaskStage)
                .ThenInclude(t => t.Assigned)
                .Include(r => r.TaskRole)
                .ThenInclude(c => c.Compiler)
                .Include(r => r.TaskRole)
                .ThenInclude(v => v.VerifierOne)
                .Include(r => r.TaskRole)
                .ThenInclude(v => v.VerifierTwo)
                .Include(r => r.TaskRole)
                .ThenInclude(h => h.HundredPercentCheck)
                .ToListAsync();


            foreach (var task in NcneTasks)
            {
                task.FormDateStatus = (int)GetDeadLineStatus(task.AnnounceDate, NcneTaskStageType.Forms, task.TaskStage);
                task.CommitDateStatus = (int)GetDeadLineStatus(task.CommitDate, NcneTaskStageType.Commit_To_Print, task.TaskStage);
                task.CisDateStatus = (int)GetDeadLineStatus(task.CisDate, NcneTaskStageType.CIS, task.TaskStage);
                task.PublishDateStatus = (int)GetDeadLineStatus(task.PublicationDate, NcneTaskStageType.Publication, task.TaskStage);

            }
        }


        public async Task<IActionResult> OnPostTaskNoteAsync(string taskNote, int processId)
        {
            taskNote = string.IsNullOrEmpty(taskNote) ? string.Empty : taskNote.Trim();

            var existingTaskNote = await _dbContext.TaskNote.FirstOrDefaultAsync(tn => tn.ProcessId == processId);
            var user = await _ncneUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);
            if (existingTaskNote == null)
            {
                if (!string.IsNullOrEmpty(taskNote))
                {
                    await _dbContext.TaskNote.AddAsync(new TaskNote()
                    {
                        ProcessId = processId,
                        Text = taskNote,
                        Created = DateTime.Now,
                        CreatedBy = user,
                        LastModified = DateTime.Now,
                        LastModifiedBy = user
                    });

                    await _dbContext.SaveChangesAsync();
                }

                await OnGetAsync();
                return Page();
            }

            existingTaskNote.Text = taskNote;
            existingTaskNote.LastModified = DateTime.Now;
            existingTaskNote.LastModifiedBy = user;
            await _dbContext.SaveChangesAsync();

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAssignTaskToUserAsync(int processId, string userName, string userPrincipal)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("ActivityName", "AssignUser");

            ValidationErrorMessages.Clear();

            if (await _ncneUserDbService.ValidateUserAsync(userName))
            {
                var instance = await _dbContext.TaskInfo
                              .Include(r => r.TaskRole)
                              .ThenInclude(u => u.Compiler)
                              .Include(r => r.TaskRole)
                              .ThenInclude(u => u.VerifierOne)
                              .Include(r => r.TaskRole)
                              .ThenInclude(u => u.VerifierTwo)
                              .Include(r => r.TaskRole)
                              .ThenInclude(u => u.HundredPercentCheck)
                              .Include(s => s.TaskStage)
                              .ThenInclude(u => u.Assigned)
                              .FirstAsync(t => t.ProcessId == processId);

                var user = await _ncneUserDbService.GetAdUserAsync(userPrincipal);
                instance.Assigned = user;
                instance.AssignedDate = DateTime.Now;

                //Update the new user in task role and stages
                UpdateTaskStageUser(instance, user);

                //Update the caris Project with the current user if the caris project is created already
                await UpdateCarisProject(processId, user);

                await _dbContext.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation($"Attempted to assign task to unknown user {userName}");
                ValidationErrorMessages.Add($"Unable to assign task to unknown user {userName}");
                return new JsonResult(ValidationErrorMessages)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }

            return StatusCode(200);
        }

        private void UpdateTaskStageUser(TaskInfo task, AdUser user)
        {
            _logger.LogInformation(
                " Updating Task stage users from roles for task {ProcessId}.");


            var taskInProgress = task.TaskStage.Find(t => t.Status == NcneTaskStageStatus.InProgress.ToString()
                                                          && t.TaskStageTypeId != (int)NcneTaskStageType.Forms);

            //Assign the user to the role of the user who is in-charge of the task stage in progress
            if (taskInProgress == null)
                task.TaskRole.HundredPercentCheck = user;
            else
            {
                switch ((NcneTaskStageType)taskInProgress.TaskStageTypeId)
                {
                    case NcneTaskStageType.With_SDRA:
                    case NcneTaskStageType.With_Geodesy:
                    case NcneTaskStageType.Specification:
                    case NcneTaskStageType.Compile:
                    case NcneTaskStageType.V1_Rework:
                    case NcneTaskStageType.V2_Rework:
                        {
                            task.TaskRole.Compiler = user;
                            break;
                        }
                    case NcneTaskStageType.V2:
                        {
                            task.TaskRole.VerifierTwo = user;
                            break;
                        }
                    case NcneTaskStageType.Hundred_Percent_Check:
                        {
                            task.TaskRole.HundredPercentCheck = user;
                            break;
                        }
                    default:
                        {
                            task.TaskRole.VerifierOne = user;
                            break;
                        }
                }
            }


            foreach (var stage in task.TaskStage)
            {
                //Assign the user according to the stage
                stage.Assigned = (NcneTaskStageType)stage.TaskStageTypeId switch
                {
                    NcneTaskStageType.With_Geodesy => task.TaskRole.Compiler,
                    NcneTaskStageType.With_SDRA => task.TaskRole.Compiler,
                    NcneTaskStageType.Specification => task.TaskRole.Compiler,
                    NcneTaskStageType.Compile => task.TaskRole.Compiler,
                    NcneTaskStageType.V1_Rework => task.TaskRole.Compiler,
                    NcneTaskStageType.V2_Rework => task.TaskRole.Compiler,
                    NcneTaskStageType.V2 => task.TaskRole.VerifierTwo,
                    NcneTaskStageType.Hundred_Percent_Check => task.TaskRole.HundredPercentCheck,
                    _ => task.TaskRole.VerifierOne
                };
            }

        }

        private async Task UpdateCarisProject(int processId, AdUser user)
        {
            var carisProject = _dbContext.CarisProjectDetails.FirstOrDefault(c => c.ProcessId == processId);

            if (carisProject != null)
            {
                var hpdUser = await GetHpdUser(user);
                await _carisProjectHelper.UpdateCarisProject(carisProject.ProjectId, hpdUser.HpdUsername,
                    _generalConfig.Value.CarisProjectTimeoutSeconds);

            }
        }

        private async Task<HpdUser> GetHpdUser(AdUser user)
        {

            try
            {
                return await _dbContext.HpdUser.SingleAsync(u => u.AdUser == user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Unable to find HPD Username for {CurrentUser.DisplayName} in our system.");
                throw new InvalidOperationException($"Unable to find HPD username for {user.DisplayName}  in our system.",
                    ex.InnerException);
            }

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

        public ncneDateStatus GetDeadLineStatus(DateTime? deadline, NcneTaskStageType taskStageTypeId, List<TaskStage> taskStages)
        {
            ncneDateStatus result = ncneDateStatus.None;

            if (taskStages.Find(t => t.TaskStageTypeId == (int)taskStageTypeId).Status ==
                NcneTaskStatus.Completed.ToString())
            {
                result = ncneDateStatus.Green;
            }
            else
            {
                if (deadline.HasValue)
                {
                    if ((deadline.Value.Date - DateTime.Today.Date).TotalDays <= 0)
                    {
                        result = ncneDateStatus.Red;
                    }
                    else
                    {
                        if ((deadline.Value.Date - DateTime.Today.Date).TotalDays <= 7)
                        {
                            result = ncneDateStatus.Amber;
                        }
                    }
                }
            }

            return result;
        }

    }
}
