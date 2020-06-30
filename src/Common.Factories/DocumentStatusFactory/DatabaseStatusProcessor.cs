using System;
using System.Threading.Tasks;
using Common.Factories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;

namespace Common.Factories.DocumentStatusFactory
{
    public class DatabaseDocumentStatusProcessor : IDocumentStatusProcessor
    {
        private readonly WorkflowDbContext _dbContext;

        public DatabaseDocumentStatusProcessor(WorkflowDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        private int Add()
        {
            throw new NotImplementedException();
        }

        public async Task<int> Update(int processId, int sourceDocumentId, SourceDocumentRetrievalStatus status)
        {
            var row = await _dbContext.DatabaseDocumentStatus
                .SingleOrDefaultAsync(r => r.ProcessId == processId
                && r.SdocId == sourceDocumentId);

            if (row == null)
            {
                throw new ApplicationException($"Could not find database document row for ProcessId {processId} and SdocId: {sourceDocumentId}");
            }

            // update
            row.Status = status.ToString();

            await _dbContext.SaveChangesAsync();
            return row.DatabaseDocumentStatusId;
        }
    }
}
