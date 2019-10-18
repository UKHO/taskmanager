using System.Threading.Tasks;

namespace Portal.HttpClients
{
    public interface IDataServiceApiClient
    {
        Task PutAssessmentCompleted(int sdocId, string comment);
    }
}