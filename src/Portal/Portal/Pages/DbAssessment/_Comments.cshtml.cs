using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Portal.Helpers;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _CommentsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ICommentsHelper _commentsHelper;

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        public List<Comments> Comments { get; set; }

        public _CommentsModel(WorkflowDbContext dbContext, ICommentsHelper commentsHelper)
        {
            _dbContext = dbContext;
            _commentsHelper = commentsHelper;
        }

        public async Task OnGetAsync(int processId)
        {
            Comments = await _dbContext.Comment.Where(c => c.ProcessId == processId).ToListAsync();
            ProcessId = processId;

        }

        public async Task<IActionResult> OnPostCommentsAsync(string newCommentMessage)
        {
            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(c => c.ProcessId == ProcessId);

            await _commentsHelper.AddComment(newCommentMessage, ProcessId, workflowInstance.WorkflowInstanceId);

            Comments = await _dbContext.Comment.Where(c => c.ProcessId == ProcessId).ToListAsync();

            return Page();
        }
    }
}