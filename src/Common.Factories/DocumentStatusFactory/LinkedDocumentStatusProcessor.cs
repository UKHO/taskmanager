using System;
using System.Threading.Tasks;
using Common.Factories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;

namespace Common.Factories.DocumentStatusFactory
{
    public class LinkedDocumentStatusProcessor : IDocumentStatusProcessor
    {
        private readonly WorkflowDbContext _dbContext;

        public LinkedDocumentStatusProcessor(WorkflowDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public int Add()
        {
            throw new NotImplementedException();
        }

        public async Task<int> Update(int processId, int sourceDocumentId, string sourceDocumentName,
            string sourceDocumentType, SourceDocumentRetrievalStatus status, Guid? correlationId = null)
        {
            var row = await _dbContext.LinkedDocument
                .SingleOrDefaultAsync(r => r.ProcessId == processId
                                           && r.LinkedSdocId == sourceDocumentId);

            if (row == null)
            {
                throw new ApplicationException($"Could not find linked document row for SdocId: {sourceDocumentId}");
            }

            // update
            row.Status = status.ToString();

            await _dbContext.SaveChangesAsync();
            return row.LinkedDocumentId;
        }
    }
}
