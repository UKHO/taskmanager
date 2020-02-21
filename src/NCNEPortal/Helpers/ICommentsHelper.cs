using System.Threading.Tasks;

namespace NCNEPortal.Helpers
{
    public interface ICommentsHelper
    {
        Task AddTaskComment(string comment, int processId, string userFullName);
        Task AddTaskStageComment(string comment, int processId, int taskStageId, string userFullName);
    }
}
