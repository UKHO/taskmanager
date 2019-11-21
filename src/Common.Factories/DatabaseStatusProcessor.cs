﻿using System;
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

        public async Task<int> Update(int processId, int sourceDocumentId, SourceDocumentRetrievalStatus status, Guid? correlationId)
        {
            var row = await _dbContext.DatabaseDocumentStatus
                .SingleOrDefaultAsync(r => r.ProcessId == processId
                && r.SdocId == sourceDocumentId);

            if (row == null)
            {
                // add
                var databaseDocumentStatus = new DatabaseDocumentStatus
                {
                    CorrelationId = correlationId,
                    ProcessId = processId,
                    SdocId = sourceDocumentId,
                    Status = status.ToString(),
                    StartedAt = DateTime.Now
                };

                await _dbContext.DatabaseDocumentStatus.AddAsync(databaseDocumentStatus);
                await _dbContext.SaveChangesAsync();
                return databaseDocumentStatus.DatabaseDocumentStatusId;
            }

            // update
            row.Status = status.ToString();
            await _dbContext.SaveChangesAsync();
            return row.DatabaseDocumentStatusId;
        }
    }
}
