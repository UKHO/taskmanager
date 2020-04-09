using System;
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
            SourceDocumentRetrievalStatus status, SourceType sourceType, Guid? correlationId,
            string sourceDocumentName = null, string sourceDocumentType = null,
            string generatedFullFilename = null)
        {
            var documentStatusProcessor = documentStatusFactory.GetDocumentStatusProcessor(sourceType);
            return await documentStatusProcessor.Update(processId, sourceDocumentId, sourceDocumentName, sourceDocumentType, status, correlationId, generatedFullFilename);
        }
    }
}