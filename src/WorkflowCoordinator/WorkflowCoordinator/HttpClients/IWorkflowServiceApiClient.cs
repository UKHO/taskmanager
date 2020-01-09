using System.Threading.Tasks;

namespace WorkflowCoordinator.HttpClients
{
    public interface IWorkflowServiceApiClient
    {
        Task<int> CreateWorkflowInstance(int dbAssessmentWorkflowId);
        Task<int> GetDBAssessmentWorkflowId();
        Task<string> GetWorkflowInstanceSerialNumber(int workflowInstanceId);
        Task TerminateWorkflowInstance(string serialNumber);
        Task<string> ProgressWorkflowInstance(int workflowInstanceId, string serialNo);
    }
}