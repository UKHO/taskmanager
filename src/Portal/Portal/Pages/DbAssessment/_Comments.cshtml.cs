using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _CommentsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        public List<Comments> Comments { get; set; }

        public _CommentsModel(WorkflowDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task OnGetAsync(int processId)
        {
            Comments = await _dbContext.Comment.Where(c => c.ProcessId == processId).ToListAsync();
            ProcessId = processId;

        }

        public async Task<IActionResult> OnPostCommentsAsync(string newCommentMessage)
        {
            // TODO: Test with Azure
            // TODO: This will not work in Azure; need alternative; but will work in local dev

            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(c => c.ProcessId == ProcessId);

            await AddComment(newCommentMessage, ProcessId, workflowInstance.WorkflowInstanceId);

            Comments = await _dbContext.Comment.Where(c => c.ProcessId == ProcessId).ToListAsync();

            return Page();
        }

        private async Task AddComment(string comment, int processId, int workflowInstanceId)
        {
            var userId = _httpContextAccessor.HttpContext.User.Identity.Name;

            await _dbContext.Comment.AddAsync(new Comments
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                Created = DateTime.Now,
                Username = string.IsNullOrEmpty(userId) ? "Unknown" : userId,
                Text = comment
            });

            await _dbContext.SaveChangesAsync();
        }

    }
}