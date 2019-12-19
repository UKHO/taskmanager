using System;
using System.Threading.Tasks;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class CommentsHelper : ICommentsHelper
    {
        private readonly WorkflowDbContext _dbContext;

        public CommentsHelper(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddComment(string comment, int processId, int workflowInstanceId, string userFullName)
        {
            await _dbContext.Comment.AddAsync(new Comment
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                Created = DateTime.Now,
                Username = userFullName,
                Text = comment
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}