using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class CommentsHelper : ICommentsHelper
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommentsHelper(WorkflowDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task AddComment(string comment, int processId, int workflowInstanceId)
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