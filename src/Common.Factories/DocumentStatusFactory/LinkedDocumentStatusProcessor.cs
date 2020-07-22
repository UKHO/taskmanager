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

        public Task<int> Update(int processId, int sourceDocumentId, SourceDocumentRetrievalStatus status)
        {
            throw new NotImplementedException();
        }

        public async Task<int> Update(int processId, int sourceDocumentId, SourceDocumentRetrievalStatus status, Guid uniqueId)
        {
            var row = await _dbContext.LinkedDocument
                .SingleOrDefaultAsync(r => r.UniqueId == uniqueId);

            if (row == null)
            {
                throw new ApplicationException($"Could not find linked document row for ProcessId {processId} and SdocId: {sourceDocumentId}");
            }

            // update
            row.Status = status.ToString();

            await _dbContext.SaveChangesAsync();
            return row.LinkedDocumentId;
        }
    }
}
