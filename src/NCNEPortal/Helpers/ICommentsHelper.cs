using NCNEPortal.Enums;
using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Threading.Tasks;

namespace NCNEPortal.Helpers
{
    public interface ICommentsHelper
    {
        Task AddTaskComment(string comment, int processId, AdUser user);
        Task AddTaskStageComment(string comment, int processId, int taskStageId, AdUser user);
        Task AddTaskSystemComment(NcneCommentType changeType, int processId, AdUser user, string stageName,
            string roleName, DateTime? dateChangedTo);
    }
}
