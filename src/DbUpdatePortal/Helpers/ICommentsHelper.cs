using DbUpdatePortal.Enums;
using DbUpdateWorkflowDatabase.EF.Models;
using System;
using System.Threading.Tasks;

namespace DbUpdatePortal.Helpers
{
    public interface ICommentsHelper
    {
        Task AddTaskComment(string comment, int processId, AdUser user);
        Task AddTaskStageComment(string comment, int processId, int taskStageId, AdUser user);
        Task AddTaskSystemComment(DbUpdateCommentType changeType, int processId, AdUser user, string stageName,
            string roleName, DateTime? dateChangedTo);
    }
}
