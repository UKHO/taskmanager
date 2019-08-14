using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCoordinator.Models;

namespace WorkflowCoordinator.HttpClients
{
    public interface IDataServiceApiClient
    {
        Task<IEnumerable<AssessmentModel>> GetAssessments(string callerCode);
    }
}