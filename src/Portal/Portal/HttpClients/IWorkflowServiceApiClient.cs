using System.Threading.Tasks;

namespace Portal.HttpClients
{
    public interface IWorkflowServiceApiClient
    {
        Task<string> GetWorkflowInstanceSerialNumber(int workflowInstanceId);
        Task TerminateWorkflowInstance(string serialNumber);
    }
}