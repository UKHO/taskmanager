using NCNEPortal.Enums;
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

        public async Task AddTaskSystemComment(NcneCommentType changeType, int processId, AdUser user, string stageName,
            string roleName, DateTime? dateChangedTo)
        {
            var comment = changeType switch
            {
                NcneCommentType.CompleteStage => stageName + " Step completed",
                NcneCommentType.ReworkStage => stageName + " Step sent for Rework",
                NcneCommentType.DateChange => "Task Information dates changed",
                NcneCommentType.CompilerChange => "Compiler role changed to " + roleName,
                NcneCommentType.V1Change => "V1 role changed to " + roleName,
                NcneCommentType.V2Change => "V2 role changed to " + roleName,
                NcneCommentType.HundredPcChange => "100% Check role changed to " + roleName,
                NcneCommentType.CarisPublish => "Chart published in CARIS",
                NcneCommentType.CompleteWorkflow => "Workflow completed",
                NcneCommentType.ThreePsChange => "3PS Details changed",
                _ => throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null),
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
