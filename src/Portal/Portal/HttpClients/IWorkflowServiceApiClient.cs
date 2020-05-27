using System.Threading.Tasks;

namespace Portal.HttpClients
{
    public interface IWorkflowServiceApiClient
    {
        Task<bool> CheckK2Connection();
        Task<string> GetTaskCurrentStage(string serialNumber);
        Task<bool> ProgressWorkflowInstance(int processId,string serialNumber, string currentTaskStage, string progressToTaskStage);
        Task<bool> RejectWorkflowInstance(int processId, string serialNumber, string currentTaskStage, string progressToTaskStage);
    }
}