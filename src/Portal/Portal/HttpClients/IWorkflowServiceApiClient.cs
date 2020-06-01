using System.Threading.Tasks;

namespace Portal.HttpClients
{
    public interface IWorkflowServiceApiClient
    {
        Task<string> GetTaskCurrentStage(string serialNumber);
        Task<bool> ProgressWorkflowInstance(int processId,string serialNumber, string currentTaskStage, string progressToTaskStage);
    }
}