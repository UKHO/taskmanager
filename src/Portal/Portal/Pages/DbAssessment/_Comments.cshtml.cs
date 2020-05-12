using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portal.BusinessLogic;
using Portal.Helpers;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _CommentsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ICommentsHelper _commentsHelper;
        private readonly IWorkflowBusinessLogicService _workflowBusinessLogicService;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<_CommentsModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        public List<Comment> Comments { get; set; }

        private string _userFullName;
        public string UserFullName
        {
            get => string.IsNullOrEmpty(_userFullName) ? "Unknown user" : _userFullName;
            private set => _userFullName = value;
        }

        public _CommentsModel(WorkflowDbContext dbContext,
            ICommentsHelper commentsHelper, 
            IWorkflowBusinessLogicService workflowBusinessLogicService,
            IAdDirectoryService adDirectoryService,
            ILogger<_CommentsModel> logger)
        {
            _dbContext = dbContext;
            _commentsHelper = commentsHelper;
            _workflowBusinessLogicService = workflowBusinessLogicService;
            _adDirectoryService = adDirectoryService;
            _logger = logger;
        }

        public async Task OnGetAsync(int processId)
        {
            Comments = await _dbContext.Comment.Where(c => c.ProcessId == processId).ToListAsync();
            ProcessId = processId;
        }

        public async Task<IActionResult> OnPostCommentsAsync(string newCommentMessage)
        {
            LogContext.PushProperty("ProcessId", ProcessId);
            LogContext.PushProperty("PortalResource", nameof(OnPostCommentsAsync));

            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(c => c.ProcessId == ProcessId);

            UserFullName = await _adDirectoryService.GetFullNameForUserAsync(this.User);
            
            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(ProcessId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(ProcessId)} {ProcessId} is readonly, cannot add comment");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} is readonly, cannot add comment");
                throw appException;
            }

            await _commentsHelper.AddComment(newCommentMessage,
                ProcessId,
                workflowInstance.WorkflowInstanceId,
                UserFullName);

            Comments = await _dbContext.Comment.Where(c => c.ProcessId == ProcessId).ToListAsync();

            return Page();
        }
    }
}