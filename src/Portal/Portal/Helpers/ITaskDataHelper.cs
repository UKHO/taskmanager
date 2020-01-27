using System.Threading.Tasks;
using WorkflowDatabase.EF.Interfaces;

namespace Portal.Helpers
{
    public interface ITaskDataHelper
    {
        Task<ITaskData> GetTaskData(string activityName, int processId);
        Task<IProductActionData> GetProductActionData(string activityName, int processId);
    }
}
