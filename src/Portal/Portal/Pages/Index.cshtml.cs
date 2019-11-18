using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Portal.ViewModels;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;

        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IList<TaskViewModel> Tasks { get; set; }

        public IndexModel(WorkflowDbContext dbContext, IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        public void OnGet()
        {
            var workflows = _dbContext.WorkflowInstance
                .Include(c => c.Comment)
                .Include(a => a.AssessmentData)
                .Include(d => d.DbAssessmentReviewData)
                .Include(t => t.TaskNote)
                .Where(wi => wi.Status == WorkflowStatus.Started.ToString())
                .ToList();

            this.Tasks = _mapper.Map<List<WorkflowInstance>, List<TaskViewModel>>(workflows);
        }

        public async Task<IActionResult> OnPostTaskNoteAsync(string taskNote, int processId)
        {
            //TODO: LOG!
            var userId = _httpContextAccessor.HttpContext.User.Identity.Name;
            var username = string.IsNullOrEmpty(userId) ? "Unknown" : userId;
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
                        CreatedByUsername = username,
                        LastModified = DateTime.Now,
                        LastModifiedByUsername = username,
                    });
                    await _dbContext.SaveChangesAsync();
                }

                OnGet();
                return StatusCode(200);
            }

            existingTaskNote.Text = taskNote;
            existingTaskNote.LastModified = DateTime.Now;
            existingTaskNote.LastModifiedByUsername = username;
            await _dbContext.SaveChangesAsync();

            OnGet();
            return StatusCode(200);
        }
    }
}
