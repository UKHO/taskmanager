using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Portal.Auth;
using Portal.ViewModels;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;

        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPortalUser _portalUser;

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        [BindProperty(SupportsGet = true)]
        public IList<TaskViewModel> Tasks { get; set; }

        public IndexModel(WorkflowDbContext dbContext,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IPortalUser portalUser)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _portalUser = portalUser;
        }

        public async Task OnGetAsync()
        {
            var workflows = await _dbContext.WorkflowInstance
                .Include(c => c.Comment)
                .Include(a => a.AssessmentData)
                .Include(d => d.DbAssessmentReviewData)
                .Include(t => t.TaskNote)
                .Where(wi => wi.Status == WorkflowStatus.Started.ToString())
                .ToListAsync();

            UserFullName = await _portalUser.GetFullNameForUser(this.User);

            this.Tasks = _mapper.Map<List<WorkflowInstance>, List<TaskViewModel>>(workflows);
        }

        public async Task<IActionResult> OnPostTaskNoteAsync(string taskNote, int processId)
        {
            UserFullName = await _portalUser.GetFullNameForUser(this.User);

            taskNote = string.IsNullOrEmpty(taskNote) ? string.Empty : taskNote.Trim();

            var existingTaskNote = await _dbContext.TaskNote.FirstOrDefaultAsync(tn => tn.ProcessId == processId);

            if (existingTaskNote == null)
            {
                if (!string.IsNullOrEmpty(taskNote))
                {
                    var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.ProcessId == processId);

                    await _dbContext.TaskNote.AddAsync(new TaskNote()
                    {
                        WorkflowInstanceId = workflowInstance.WorkflowInstanceId,
                        ProcessId = processId,
                        Text = taskNote,
                        Created = DateTime.Now,
                        CreatedByUsername = UserFullName,
                        LastModified = DateTime.Now,
                        LastModifiedByUsername = UserFullName,
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
    }
}
