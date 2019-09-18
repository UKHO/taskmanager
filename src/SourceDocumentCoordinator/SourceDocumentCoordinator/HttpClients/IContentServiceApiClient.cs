using System;
using System.Threading.Tasks;

namespace SourceDocumentCoordinator.HttpClients
{
    public interface IContentServiceApiClient
    {
        Task<Guid> Post();
    }
}
