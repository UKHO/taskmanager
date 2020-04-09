using System;
using System.Threading.Tasks;
using WorkflowDatabase.EF;

namespace Common.Factories.Interfaces
{
    public interface IDocumentStatusProcessor
    {
        Task<int> Update(int processId, int sourceDocumentId, string sourceDocumentName, string sourceDocumentType,
            SourceDocumentRetrievalStatus status, Guid? correlationId = null,
            string generatedFullFilename = null);
    }
}
