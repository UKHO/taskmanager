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

        public IndexModel(INcneUserDbService ncneUserDbService,
                          NcneWorkflowDbContext ncneWorkflowDbContext,
                          ILogger<IndexModel> logger,
                          IAdDirectoryService adDirectoryService)
        {
            _ncneUserDbService = ncneUserDbService;
            _dbContext = ncneWorkflowDbContext;
            _logger = logger;
            _adDirectoryService = adDirectoryService;
        }

        public async Task OnGetAsync()
        {
            NcneTasks = await _dbContext.TaskInfo
                .Include(c => c.TaskNote)
                .Include(s => s.TaskStage)
                .Include(r => r.TaskRole)
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

            if (existingTaskNote == null)
            {
                if (!string.IsNullOrEmpty(taskNote))
                {
                    await _dbContext.TaskNote.AddAsync(new TaskNote()
                    {
                        ProcessId = processId,
                        Text = taskNote,
                        Created = DateTime.Now,
                        CreatedByUsername = CurrentUser.DisplayName,
                        LastModified = DateTime.Now,
                        LastModifiedByUsername = CurrentUser.DisplayName
                    });

                    await _dbContext.SaveChangesAsync();
                }

                await OnGetAsync();
                return Page();
            }

            existingTaskNote.Text = taskNote;
            existingTaskNote.LastModified = DateTime.Now;
            existingTaskNote.LastModifiedByUsername = CurrentUser.DisplayName;
            await _dbContext.SaveChangesAsync();

            await OnGetAsync();
            return Page();
        }

        public async Task OnPostAssignTaskToUserAsync(int processId, string userName, string taskStage)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("ActivityName", "AssignUser");

            if (await _ncneUserDbService.ValidateUserAsync(userName))
            {
                var instance = await _dbContext.TaskInfo.FirstAsync(t => t.ProcessId == processId);

                instance.AssignedUser = userName;
                instance.AssignedDate = DateTime.Now;

                await _dbContext.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation($"Attempted to assign task to unknown user {userName}");
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
