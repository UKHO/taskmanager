using System.Threading.Tasks;
using WorkflowDatabase.EF;

namespace Common.Factories.Interfaces
{
    public interface IDocumentStatusProcessor
    {
        Task<int> Update(int processId, int sourceDocumentId, SourceDocumentRetrievalStatus status);
    }
}
