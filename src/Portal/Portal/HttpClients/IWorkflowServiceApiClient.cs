using System.Threading.Tasks;

namespace Portal.HttpClients
{
    public interface IWorkflowServiceApiClient
    {
        Task TerminateWorkflowInstance(string serialNumber);
        Task<bool> CheckK2Connection();
    }
}