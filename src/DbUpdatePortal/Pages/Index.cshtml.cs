using Common.Helpers.Auth;
using DbUpdatePortal.Auth;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
                          ILogger<IndexModel> logger
                          , IAdDirectoryService adDirectoryService
                          )
        {
            _dbUpdateUserDbService = dbUpdateUserDbService;
            _dbContext = dbContext;
            _logger = logger;
            _adDirectoryService = adDirectoryService;

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
                var instance = await _dbContext.TaskInfo.FirstAsync(t => t.ProcessId == processId);
                var user = await _dbUpdateUserDbService.GetAdUserAsync(userPrinciple);

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
                (await _dbUpdateUserDbService.GetUsersFromDbAsync()).Select(u => new
                {
                    u.DisplayName,
                    u.UserPrincipalName
                });

            return new JsonResult(users);
        }
    }
}
