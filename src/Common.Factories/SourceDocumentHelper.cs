using System.Threading.Tasks;
using Common.Factories.Interfaces;
using Common.Messages.Enums;
using WorkflowDatabase.EF;

namespace Common.Factories
{
    public class SourceDocumentHelper
    {
        public static async Task<int> UpdateSourceDocumentStatus(IDocumentStatusFactory documentStatusFactory,
            int processId, int sourceDocumentId,
            SourceDocumentRetrievalStatus status, SourceDocumentType docType)
        {
            if (docType != SourceDocumentType.Primary) return sourceDocumentId;

            var documentStatusProcessor = documentStatusFactory.GetDocumentStatusProcessor(docType);
            return await documentStatusProcessor.Update(processId, sourceDocumentId, status);
        }
    }
}