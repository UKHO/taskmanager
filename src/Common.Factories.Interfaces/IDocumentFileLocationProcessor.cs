using System;
using System.Threading.Tasks;

namespace Common.Factories.Interfaces
{
    public interface IDocumentFileLocationProcessor
    {
        Task<int> Update(int processId, int sourceDocumentId, Guid contentServiceId, string generatedFullFilename);
    }
}
