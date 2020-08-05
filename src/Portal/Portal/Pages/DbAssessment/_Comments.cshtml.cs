using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers.Auth;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class _CommentsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ICommentsHelper _dbAssessmentCommentsHelper;
        private readonly IWorkflowBusinessLogicService _workflowBusinessLogicService;
        private readonly IAdDirectoryService _adDirectoryService;
        private readonly ILogger<_CommentsModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        public List<Comment> Comments { get; set; }

        private (string DisplayName, string UserPrincipalName) _currentUser;
        public (string DisplayName, string UserPrincipalName) CurrentUser
        {
            get
            {
                if (_currentUser == default) _currentUser = _adDirectoryService.GetUserDetails(this.User);
                return _currentUser;
            }
        }

        public _CommentsModel(WorkflowDbContext dbContext,
            ICommentsHelper dbAssessmentCommentsHelper,
            IWorkflowBusinessLogicService workflowBusinessLogicService,
            IAdDirectoryService adDirectoryService,
            ILogger<_CommentsModel> logger)
        {
            _dbContext = dbContext;
            _dbAssessmentCommentsHelper = dbAssessmentCommentsHelper;
            _workflowBusinessLogicService = workflowBusinessLogicService;
            _adDirectoryService = adDirectoryService;
            _logger = logger;
        }

        public async Task OnGetAsync(int processId)
        {
            Comments = await _dbContext.Comments.Where(c => c.ProcessId == processId).ToListAsync();
            ProcessId = processId;
        }

        public async Task<IActionResult> OnPostCommentsAsync(string newCommentMessage)
        {
            LogContext.PushProperty("ProcessId", ProcessId);
            LogContext.PushProperty("PortalResource", nameof(OnPostCommentsAsync));

            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(c => c.ProcessId == ProcessId);

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(ProcessId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(ProcessId)} {ProcessId} is readonly, cannot add comment");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} is readonly, cannot add comment");
                throw appException;
            }

            try
            {
                await _dbAssessmentCommentsHelper.AddComment(newCommentMessage,
                    ProcessId,
                    workflowInstance.WorkflowInstanceId,
                    CurrentUser.UserPrincipalName);
            }
            catch (ApplicationException appException)
            {
                _logger.LogError(appException, "Problem adding comment for ProcessId {ProcessId}.");
                // TODO  - shall we let user know the comment was not added?
            }

            Comments = await _dbContext.Comments.Where(c => c.ProcessId == ProcessId).ToListAsync();

            return Page();
        }
    }
}