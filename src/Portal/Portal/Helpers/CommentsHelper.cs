using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Portal.Auth;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class CommentsHelper : ICommentsHelper
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IPortalUser _portalUser;
        private readonly IHttpContextAccessor _httpContext;

        public CommentsHelper(WorkflowDbContext dbContext, IPortalUser portalUser, IHttpContextAccessor httpContext)
        {
            _dbContext = dbContext;
            _portalUser = portalUser;
            _httpContext = httpContext;
        }

        public async Task AddComment(string comment, int processId, int workflowInstanceId)
        {
            var userFullName = await _portalUser.GetFullNameForUser(_httpContext.HttpContext.User);

            await _dbContext.Comment.AddAsync(new Comments
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                Created = DateTime.Now,
                Username = string.IsNullOrEmpty(userFullName) ? "Unknown user" : userFullName,
                Text = comment
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}