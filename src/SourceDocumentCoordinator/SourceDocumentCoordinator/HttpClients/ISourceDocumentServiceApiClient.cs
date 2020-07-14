using System;
using System.Threading.Tasks;

namespace SourceDocumentCoordinator.HttpClients
{
    public interface ISourceDocumentServiceApiClient
    {
        Task<Guid> Post(int processId, int sdocId, string filepath);
    }
}
