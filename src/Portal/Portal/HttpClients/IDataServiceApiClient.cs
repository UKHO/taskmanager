using System.Threading.Tasks;
using DataServices.Models;

namespace Portal.HttpClients
{
    public interface IDataServiceApiClient
    {
        Task PutAssessmentCompleted(int sdocId, string comment);
        Task<DocumentAssessmentData> GetAssessmentData(int sdocId);
    }
}