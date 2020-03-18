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
using System.Threading.Tasks;


namespace NCNEPortal.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IUserIdentityService _userIdentityService;
        private readonly NcneWorkflowDbContext _dbContext;
        private readonly ILogger<IndexModel> _logger;
        private readonly IDirectoryService _directoryService;


        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        [BindProperty(SupportsGet = true)]
        public List<TaskInfo> NcneTasks { get; set; }

        public IndexModel(IUserIdentityService userIdentityService,
                          NcneWorkflowDbContext ncneWorkflowDbContext,
                          ILogger<IndexModel> logger,
                          IDirectoryService directoryService)
        {
            _userIdentityService = userIdentityService;
            _dbContext = ncneWorkflowDbContext;
            _logger = logger;
            _directoryService = directoryService;
        }

        public async Task OnGetAsync()
        {
            NcneTasks = await _dbContext.TaskInfo
                .Include(c => c.TaskNote)
                .Include(s => s.TaskStage)
                .ToListAsync();


            foreach (var task in NcneTasks)
            {
                if (task.TaskStage.Find(t => t.TaskStageTypeId == (int)NcneTaskStageType.Forms).Status ==
                    NcneTaskStatus.Completed.ToString())
                {
                    task.FormDateStatus = (int)ncneDateStatus.Green;
                }
                else
                {
                    if (task.AnnounceDate != null)
                    {
                        if ((Convert.ToDateTime(task.AnnounceDate) - DateTime.Today).TotalDays <= 0)
                        {
                            task.FormDateStatus = (int)ncneDateStatus.Red;
                        }
                        else
                        {
                            if ((Convert.ToDateTime(task.AnnounceDate) - DateTime.Today).TotalDays <= 7)
                            {
                                task.FormDateStatus = (int)ncneDateStatus.Amber;
                            }
                        }
                    }
                }

                if (task.TaskStage.Find(t => t.TaskStageTypeId == (int)NcneTaskStageType.Commit_To_Print).Status ==
                    NcneTaskStatus.Completed.ToString())
                {
                    task.CommitDateStatus = (int)ncneDateStatus.Green;
                }
                else
                {
                    if (task.CommitDate != null)
                    {
                        if ((Convert.ToDateTime(task.CommitDate) - DateTime.Today).TotalDays <= 0)
                        {
                            task.CommitDateStatus = (int)ncneDateStatus.Red;
                        }
                        else
                        {
                            if ((Convert.ToDateTime(task.CommitDate) - DateTime.Today).TotalDays <= 7)
                            {
                                task.CommitDateStatus = (int)ncneDateStatus.Amber;
                            }
                        }
                    }
                }

                if (task.TaskStage.Find(t => t.TaskStageTypeId == (int)NcneTaskStageType.CIS).Status ==
                    NcneTaskStatus.Completed.ToString())
                {
                    task.CisDateStatus = (int)ncneDateStatus.Green;
                }
                else
                {
                    if (task.CisDate != null)
                    {
                        if ((Convert.ToDateTime(task.CisDate) - DateTime.Today).TotalDays <= 0)
                        {
                            task.CisDateStatus = (int)ncneDateStatus.Red;
                        }
                        else
                        {
                            if ((Convert.ToDateTime(task.CisDate) - DateTime.Today).TotalDays <= 7)
                            {
                                task.CisDateStatus = (int)ncneDateStatus.Amber;
                            }
                        }
                    }
                }

                if (task.TaskStage.Find(t => t.TaskStageTypeId == (int)NcneTaskStageType.Publication).Status ==
                    NcneTaskStatus.Completed.ToString())
                {
                    task.PublishDateStatus = (int)ncneDateStatus.Green;
                }
                else
                {
                    if (task.PublicationDate != null)
                    {
                        if ((Convert.ToDateTime(task.PublicationDate) - DateTime.Today).TotalDays <= 0)
                        {
                            task.PublishDateStatus = (int)ncneDateStatus.Red;
                        }
                        else
                        {
                            if ((Convert.ToDateTime(task.PublicationDate) - DateTime.Today).TotalDays <= 7)
                            {
                                task.PublishDateStatus = (int)ncneDateStatus.Amber;
                            }
                        }
                    }
                }

            }


            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

        }


        public async Task<IActionResult> OnPostTaskNoteAsync(string taskNote, int processId)
        {
            UserFullName = await _userIdentityService.GetFullNameForUser(this.User);

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
                        CreatedByUsername = UserFullName,
                        LastModified = DateTime.Now,
                        LastModifiedByUsername = UserFullName
                    });

                    await _dbContext.SaveChangesAsync();
                }

                await OnGetAsync();
                return Page();
            }

            existingTaskNote.Text = taskNote;
            existingTaskNote.LastModified = DateTime.Now;
            existingTaskNote.LastModifiedByUsername = UserFullName;
            await _dbContext.SaveChangesAsync();

            await OnGetAsync();
            return Page();

        }


        public async Task OnPostAssignTaskToUserAsync(int processId, string userName, string taskStage)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("ActivityName", "AssignUser");

            if (await _userIdentityService.ValidateUser(userName))
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
            return new JsonResult(await _directoryService.GetGroupMembers());
        }

    }
}
