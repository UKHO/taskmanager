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
            SourceDocumentRetrievalStatus status, SourceType sourceType, Guid uniqueId)
        {
            var documentStatusProcessor = documentStatusFactory.GetDocumentStatusProcessor(sourceType);

            if (sourceType == SourceType.Linked)
            {
                return await documentStatusProcessor.Update(processId, sourceDocumentId, status, uniqueId);
            }

            return await documentStatusProcessor.Update(processId, sourceDocumentId, status);
        }

    }
}