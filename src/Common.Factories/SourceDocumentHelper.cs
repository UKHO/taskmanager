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
            SourceDocumentRetrievalStatus status, SourceType sourceType)
        {
            var documentStatusProcessor = documentStatusFactory.GetDocumentStatusProcessor(sourceType);
            return await documentStatusProcessor.Update(processId, sourceDocumentId, status);
        }

        public static async Task<int> UpdateSourceDocumentFileLocation(IDocumentFileLocationFactory documentFileLocationFactory,
            int processId, int sourceDocumentId, SourceType sourceType, Guid contentServiceId, string generatedFullFilename)
        {
            var documentStatusProcessor = documentFileLocationFactory.GetDocumentFileLocationProcessor(sourceType);
            return await documentStatusProcessor.Update(processId, sourceDocumentId, contentServiceId, generatedFullFilename);
        }

    }
}