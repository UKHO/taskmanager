using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Threading.Tasks;

namespace NCNEPortal.Helpers
{
    public class CommentsHelper : ICommentsHelper
    {
        private readonly NcneWorkflowDbContext _dbContext;

        public CommentsHelper(NcneWorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task AddTaskComment(string comment, int processId, string userFullName)
        {
            await _dbContext.TaskComment.AddAsync
            (new TaskComment
            {
                ProcessId = processId,
                Username = userFullName,
                Comment = comment,
                ActionIndicator = false,
                Created = DateTime.Now
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task AddTaskStageComment(string comment, int processId, int taskStageId, string userFullName)
        {
            await _dbContext.TaskStageComment.AddAsync(
                new TaskStageComment
                {
                    Comment = comment,
                    ProcessId = processId,
                    Created = DateTime.Now,
                    TaskStageId = taskStageId,
                    Username = userFullName
                });
        }
    }
}
