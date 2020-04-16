using System;
using System.IO;
using System.Threading.Tasks;
using Common.Factories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;

namespace Common.Factories.DocumentFileLocationFactory
{
    public class DatabaseDocumentFileLocationProcessor : IDocumentFileLocationProcessor
    {
        private readonly WorkflowDbContext _dbContext;

        public DatabaseDocumentFileLocationProcessor(WorkflowDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public async Task<int> Update(int processId, int sourceDocumentId, Guid contentServiceId, string generatedFullFilename)
        {

            if (string.IsNullOrWhiteSpace(generatedFullFilename))
            {
                throw new ArgumentException($"Source document filename and path was not supplied for ProcessId {processId} and SourceDocumentId {sourceDocumentId}", nameof(generatedFullFilename));
            }

            var row = await _dbContext.DatabaseDocumentStatus
                .SingleOrDefaultAsync(r => r.ProcessId == processId
                && r.SdocId == sourceDocumentId);

            if (row == null)
            {
                throw new ApplicationException($"Could not find database document row for ProcessId: {processId}, SdocId: {sourceDocumentId}");
            }

            row.ContentServiceId = contentServiceId;
            row.Filename = Path.GetFileName(generatedFullFilename).Trim();
            row.Filepath = Path.GetDirectoryName(generatedFullFilename)?.Trim();

            await _dbContext.SaveChangesAsync();
            return row.DatabaseDocumentStatusId;
        }
    }
}
