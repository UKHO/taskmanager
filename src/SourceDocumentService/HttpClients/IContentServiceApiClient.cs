using System;
using System.Threading.Tasks;

namespace SourceDocumentService.HttpClients
{
    public interface IContentServiceApiClient
    {
        Task<Guid> Post(byte[] fileBytes, string filename);
    }
}
