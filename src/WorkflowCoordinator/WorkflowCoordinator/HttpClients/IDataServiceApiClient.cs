using System.Collections.Generic;
using System.Threading.Tasks;
using DataServices.Models;

namespace WorkflowCoordinator.HttpClients
{
    public interface IDataServiceApiClient
    {
        Task<IEnumerable<DocumentObject>> GetAssessments(string callerCode);

        Task<DocumentAssessmentData> GetAssessmentData(string callerCode, int sdocId);
    }
}