using System;
using Common.Factories.Interfaces;
using Common.Messages.Enums;
using WorkflowDatabase.EF;

namespace Common.Factories.DocumentFileLocationFactory
{
    public class DocumentFileLocationFactory : IDocumentFileLocationFactory
    {
        private readonly WorkflowDbContext _dbContext;

        public DocumentFileLocationFactory(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IDocumentFileLocationProcessor GetDocumentFileLocationProcessor(SourceType sourceType)
        {
            switch (sourceType)
            {
                case SourceType.Primary:
                    return new PrimaryDocumentFileLocationProcessor(_dbContext);
                case SourceType.Linked:
                    return new LinkedDocumentFileLocationProcessor(_dbContext);
                case SourceType.Database:
                    return new DatabaseDocumentFileLocationProcessor(_dbContext);
                default:
                    throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null);
            }
        }
    }
}
