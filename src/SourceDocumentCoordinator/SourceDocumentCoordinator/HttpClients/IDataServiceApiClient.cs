using System.Threading.Tasks;
using DataServices.Models;

namespace SourceDocumentCoordinator.HttpClients
{
    public interface IDataServiceApiClient
    {
        Task<LinkedDocuments> GetBackwardDocumentLinks(int sdocId);
        Task<LinkedDocuments> GetForwardDocumentLinks(int sdocId);
        Task<DocumentObjects> GetSepDocumentLinks(int sdocId);
        Task<ReturnCode> GetDocumentForViewing(string callerCode, int sdocId, string writableFolderName, bool imageAsGeotiff);
        Task<bool> CheckDataServicesConnection();
        Task<QueuedDocumentObjects> GetDocumentRequestQueueStatus(string callerCode);
        Task<ReturnCode> DeleteDocumentRequestJobFromQueue(string callerCode, int sdocId, string writeableFolderName);
        Task<DocumentObjects> GetDocumentsFromList(int[] linkedDocsId);
    }
}