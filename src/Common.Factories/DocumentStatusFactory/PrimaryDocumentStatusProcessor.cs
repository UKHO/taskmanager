﻿using System;
using System.Threading.Tasks;
using Common.Factories.Interfaces;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Common.Factories.DocumentStatusFactory
{
    public class PrimaryDocumentStatusProcessor : IDocumentStatusProcessor
    {
        private readonly WorkflowDbContext _dbContext;

        public PrimaryDocumentStatusProcessor(WorkflowDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        private int Add()
        {
            throw new NotImplementedException();
        }

        public async Task<int> Update(int processId, int sourceDocumentId, SourceDocumentRetrievalStatus status)
        {
            var row = await _dbContext.PrimaryDocumentStatus
                .SingleOrDefaultAsync(r => r.ProcessId == processId
                && r.SdocId == sourceDocumentId);

            if (row == null)
            {
                // add
                var primaryDocumentStatus = new PrimaryDocumentStatus
                {
                    CorrelationId = correlationId,
                    ProcessId = processId,
                    SdocId = sourceDocumentId,
                    Status = status.ToString(),
                    StartedAt = DateTime.Now
                };

                await _dbContext.PrimaryDocumentStatus.AddAsync(primaryDocumentStatus);
                await _dbContext.SaveChangesAsync();
                return primaryDocumentStatus.PrimaryDocumentStatusId;
            }

            // update
            row.Status = status.ToString();

            await _dbContext.SaveChangesAsync();
            return row.PrimaryDocumentStatusId;
        }
    }
}
