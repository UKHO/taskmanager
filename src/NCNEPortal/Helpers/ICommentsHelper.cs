using NCNEPortal.Enums;
using System;
using System.Threading.Tasks;

namespace NCNEPortal.Helpers
{
    public interface ICommentsHelper
    {
        Task AddTaskComment(string comment, int processId, string userFullName);
        Task AddTaskStageComment(string comment, int processId, int taskStageId, string userFullName);
        Task AddTaskSystemComment(NcneCommentType changeType, int processId, string userFullName, string stageName,
            string roleName, DateTime? dateChangedTo);
    }
}
