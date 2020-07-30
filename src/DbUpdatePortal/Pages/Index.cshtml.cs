using Common.Helpers;
using Common.Helpers.Auth;
using DbUpdatePortal.Auth;
using DbUpdatePortal.Configuration;
using DbUpdatePortal.Enums;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace DbUpdatePortal.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDbUpdateUserDbService _dbUpdateUserDbService;
        private readonly DbUpdateWorkflowDbContext _dbContext;
        private readonly ILogger<IndexModel> _logger;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ICarisProjectHelper _carisProjectHelper;
        private readonly IOptions<GeneralConfig> _generalConfig;

        private (string DisplayName, string UserPrincipalName) _currentUser;

        public List<string> ValidationErrorMessages { get; set; }


        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }
        [BindProperty(SupportsGet = true)]
        public List<TaskInfo> DbUpdateTasks { get; set; }

        public IndexModel(IDbUpdateUserDbService dbUpdateUserDbService,
                          DbUpdateWorkflowDbContext dbContext,
                          ILogger<IndexModel> logger,
                           IAdDirectoryService adDirectoryService,
                          ICarisProjectHelper carisProjectHelper,
                          IOptions<GeneralConfig> generalConfig
                          )
        {
            _dbUpdateUserDbService = dbUpdateUserDbService;
            _dbContext = dbContext;
            _logger = logger;
            _adDirectoryService = adDirectoryService;
            _carisProjectHelper = carisProjectHelper;
            _generalConfig = generalConfig;

            ValidationErrorMessages = new List<string>();

        }

        public async Task OnGetAsync()
        {
            DbUpdateTasks = await _dbContext.TaskInfo
               .Include(s => s.TaskStage)
                    .ThenInclude(t => t.Assigned)
               .Include(t => t.Assigned)
               .Include(n => n.TaskNote)
                    .ThenInclude(t => t.CreatedBy)
               .Include(n => n.TaskNote)
                    .ThenInclude(u => u.LastModifiedBy)
               .Include(r => r.TaskRole)
                .ThenInclude(t => t.Compiler)
               .Include(r => r.TaskRole)
                .ThenInclude(t => t.Verifier)
               .ToListAsync();
        }
        public async Task<IActionResult> OnPostTaskNoteAsync(string taskNote, int processId)
        {
            taskNote = string.IsNullOrEmpty(taskNote) ? string.Empty : taskNote.Trim();

            var existingTaskNote = await _dbContext.TaskNote.FirstOrDefaultAsync(tn => tn.ProcessId == processId);
            var user = await _dbUpdateUserDbService.GetAdUserAsync(CurrentUser.UserPrincipalName);

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


        public async Task<IActionResult> OnPostAssignTaskToUserAsync(int processId, string userName, string userPrinciple)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("ActivityName", "AssignUser");

            ValidationErrorMessages.Clear();

            if (await _dbUpdateUserDbService.ValidateUserAsync(userName))
            {

                var instance = await _dbContext.TaskInfo
                    .Include(r => r.TaskRole)
                    .ThenInclude(u => u.Compiler)
                    .Include(r => r.TaskRole)
                    .ThenInclude(u => u.Verifier)
                    .Include(s => s.TaskStage)
                    .ThenInclude(u => u.Assigned)
                    .FirstAsync(t => t.ProcessId == processId);

                var user = await _dbUpdateUserDbService.GetAdUserAsync(userPrinciple);

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


            var taskInProgress = task.TaskStage.Find(t => t.Status == DbUpdateTaskStageStatus.InProgress.ToString());

            //Assign the user to the role of the user who is in-charge of the task stage in progress
            if (taskInProgress == null)
                task.TaskRole.Compiler = user;
            else
            {
                switch ((DbUpdateTaskStageType)taskInProgress.TaskStageTypeId)
                {
                    case DbUpdateTaskStageType.Compile:
                    case DbUpdateTaskStageType.V1_Rework:
                    case DbUpdateTaskStageType.CPT:
                    case DbUpdateTaskStageType.DCPT:
                        {
                            task.TaskRole.Compiler = user;
                            break;
                        }
                    case DbUpdateTaskStageType.V1:
                        {
                            task.TaskRole.Verifier = user;
                            break;
                        }
                    default:
                        {
                            task.TaskRole.Verifier = user;
                            break;
                        }
                }
            }


            foreach (var stage in task.TaskStage.Where(s => s.Status != DbUpdateTaskStageStatus.Completed.ToString()))
            {
                //Assign the user according to the stage
                stage.Assigned = (DbUpdateTaskStageType)stage.TaskStageTypeId switch
                {
                    DbUpdateTaskStageType.V1 => task.TaskRole.Verifier,
                    _ => task.TaskRole.Compiler
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
                    _generalConfig.Value.CarisProjectTimeoutSeconds
                    );

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
                (await _dbUpdateUserDbService.GetUsersFromDbAsync()).Select(u => new
                {
                    u.DisplayName,
                    u.UserPrincipalName
                });

            return new JsonResult(users);
        }
    }
}
