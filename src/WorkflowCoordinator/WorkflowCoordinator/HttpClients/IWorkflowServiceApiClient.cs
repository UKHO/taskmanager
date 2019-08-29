using System.Threading.Tasks;

namespace WorkflowCoordinator.HttpClients
{
    public interface IWorkflowServiceApiClient
    {
        Task<int> CreateWorkflowInstance(int dbAssessmentWorkflowId);
        Task<int> GetDBAssessmentWorkflowId();
    }
}