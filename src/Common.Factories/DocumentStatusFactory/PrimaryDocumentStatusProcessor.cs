using System;
using System.Threading.Tasks;
using Common.Factories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;

namespace Common.Factories.DocumentStatusFactory
{
    public class PrimaryDocumentStatusProcessor : IDocumentStatusProcessor
    {
        private readonly WorkflowDbContext _dbContext;

        public PrimaryDocumentStatusProcessor(WorkflowDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public async Task<int> Update(int processId, int sourceDocumentId, SourceDocumentRetrievalStatus status)
        {
            var row = await _dbContext.PrimaryDocumentStatus
                .SingleOrDefaultAsync(r => r.ProcessId == processId
                && r.SdocId == sourceDocumentId);

            if (row == null)
            {
                throw new ApplicationException($"Could not find primary document row for ProcessId: {processId} and SdocId: {sourceDocumentId}");
            }

            // update
            row.Status = status.ToString();

            await _dbContext.SaveChangesAsync();
            return row.PrimaryDocumentStatusId;
        }
    }
}
