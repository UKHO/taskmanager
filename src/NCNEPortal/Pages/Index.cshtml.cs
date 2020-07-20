using Common.Helpers.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
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
                          IAdDirectoryService adDirectoryService)
        {
            _ncneUserDbService = ncneUserDbService;
            _dbContext = ncneWorkflowDbContext;
            _logger = logger;
            _adDirectoryService = adDirectoryService;

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

        public async Task<IActionResult> OnPostAssignTaskToUserAsync(int processId, string userName, string userPrinciple)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("ActivityName", "AssignUser");

            ValidationErrorMessages.Clear();

            if (await _ncneUserDbService.ValidateUserAsync(userName))
            {
                var instance = await _dbContext.TaskInfo.FirstAsync(t => t.ProcessId == processId);
                var user = await _ncneUserDbService.GetAdUserAsync(userPrinciple);
                instance.Assigned = user;
                instance.AssignedDate = DateTime.Now;

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
