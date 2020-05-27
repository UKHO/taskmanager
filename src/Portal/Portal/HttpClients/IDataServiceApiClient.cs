using System.Threading.Tasks;
using DataServices.Models;

namespace Portal.HttpClients
{
    public interface IDataServiceApiClient
    {
        Task<DocumentAssessmentData> GetAssessmentData(int sdocId);
    }
}