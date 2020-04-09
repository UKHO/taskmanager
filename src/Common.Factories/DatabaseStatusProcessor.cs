using System;
using System.IO;
using System.Threading.Tasks;
using Common.Factories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Common.Factories
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

        public async Task<int> Update(int processId, int sourceDocumentId, string sourceDocumentName,
            string sourceDocumentType, SourceDocumentRetrievalStatus status, Guid? correlationId = null,
            string generatedFullFilename = null)
        {
            var row = await _dbContext.DatabaseDocumentStatus
                .SingleOrDefaultAsync(r => r.ProcessId == processId
                && r.SdocId == sourceDocumentId);

            if (row == null)
            {
                // add
                var databaseDocumentStatus = new DatabaseDocumentStatus
                {
                    ProcessId = processId,
                    SdocId = sourceDocumentId,
                    SourceDocumentName = sourceDocumentName,
                    SourceDocumentType = sourceDocumentType,
                    Status = status.ToString(),
                    Created = DateTime.Now
                };

                await _dbContext.DatabaseDocumentStatus.AddAsync(databaseDocumentStatus);
                await _dbContext.SaveChangesAsync();
                return databaseDocumentStatus.DatabaseDocumentStatusId;
            }

            // update
            row.Status = status.ToString();

            if (status == SourceDocumentRetrievalStatus.FileGenerated && !string.IsNullOrWhiteSpace(generatedFullFilename))
            {
                row.Filename = Path.GetFileName(generatedFullFilename).Trim();
                row.Filepath = Path.GetDirectoryName(generatedFullFilename)?.Trim();
            }

            await _dbContext.SaveChangesAsync();
            return row.DatabaseDocumentStatusId;
        }
    }
}
