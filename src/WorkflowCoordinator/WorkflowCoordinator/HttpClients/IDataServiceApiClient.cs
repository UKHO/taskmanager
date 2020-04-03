using System.Collections.Generic;
using System.Threading.Tasks;
using DataServices.Models;

namespace WorkflowCoordinator.HttpClients
{
    public interface IDataServiceApiClient
    {
        Task<IEnumerable<DocumentObject>> GetAssessments(string callerCode);
        Task<DocumentAssessmentData> GetAssessmentData(string callerCode, int sdocId);
        Task MarkAssessmentAsCompleted(int sdocId, string comment);
        Task MarkAssessmentAsAssessed(string transactionId, int sdocId, string actionType, string change);

        Task<bool> CheckDataServicesConnection();
    }
}