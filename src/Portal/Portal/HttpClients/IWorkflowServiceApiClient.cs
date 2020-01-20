using System.Threading.Tasks;
using Portal.Models;

namespace Portal.HttpClients
{
    public interface IWorkflowServiceApiClient
    {
        Task TerminateWorkflowInstance(string serialNumber);
        Task<bool> CheckK2Connection();
        Task<string> GetTaskCurrentStage(string serialNumber);
        Task<bool> ProgressWorkflowInstance(int processId,string serialNumber, string currentTaskStage, string progressToTaskStage);
        Task<K2Task> GetWorkflowInstanceData(int workflowInstanceId);
    }
}