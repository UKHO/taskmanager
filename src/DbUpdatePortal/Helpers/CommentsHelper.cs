using DbUpdatePortal.Enums;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using System;
using System.Threading.Tasks;

namespace DbUpdatePortal.Helpers
{
    public class CommentsHelper : ICommentsHelper
    {
        private readonly DbUpdateWorkflowDbContext _dbContext;

        public CommentsHelper(DbUpdateWorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddTaskComment(string comment, int processId, AdUser user)
        {
            await _dbContext.TaskComment.AddAsync
            (new TaskComment
            {
                ProcessId = processId,
                AdUser = user,
                Comment = comment,
                ActionIndicator = false,
                Created = DateTime.Now
            });

            await _dbContext.SaveChangesAsync();
        }

        public async Task AddTaskSystemComment(DbUpdateCommentType changeType, int processId, AdUser user, string stageName,
            string roleName, DateTime? dateChangedTo)
        {
            var comment = changeType switch
            {
                DbUpdateCommentType.CompleteStage => stageName + " Step completed",
                DbUpdateCommentType.ReworkStage => stageName + " Step sent for Rework",
                DbUpdateCommentType.DateChange => "Task Information dates changed",
                DbUpdateCommentType.CompilerChange => "Compiler role changed to " + roleName,
                DbUpdateCommentType.V1Change => "Verifier role changed to " + roleName,
                DbUpdateCommentType.CompleteWorkflow => "Workflow completed",
                DbUpdateCommentType.CarisProjectCreation => "Caris Project created by : " + roleName,
                _ => throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null)
            };

            await _dbContext.TaskComment.AddAsync
            (new TaskComment
            {
                ProcessId = processId,
                AdUser = user,
                Comment = comment,
                ActionIndicator = true,
                Created = DateTime.Now
            });
        }

        public async Task AddTaskStageComment(string comment, int processId, int taskStageId, AdUser user)
        {
            await _dbContext.TaskStageComment.AddAsync(
                new TaskStageComment
                {
                    Comment = comment,
                    ProcessId = processId,
                    Created = DateTime.Now,
                    TaskStageId = taskStageId,
                    AdUser = user
                });
        }
    }
}
