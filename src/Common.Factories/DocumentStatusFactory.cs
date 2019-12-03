using System;
using Common.Factories.Interfaces;
using Common.Messages.Enums;
using WorkflowDatabase.EF;

namespace Common.Factories
{
    public class DocumentStatusFactory : IDocumentStatusFactory
    {
        private readonly WorkflowDbContext _dbContext;

        public DocumentStatusFactory(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IDocumentStatusProcessor GetDocumentStatusProcessor(SourceType sourceType)
        {
            switch (sourceType)
            {
                case SourceType.Primary:
                    return new PrimaryDocumentStatusProcessor(_dbContext);
                case SourceType.Linked:
                    return new LinkedDocumentStatusProcessor(_dbContext);
                case SourceType.Database:
                    return new DatabaseDocumentStatusProcessor(_dbContext);
                default:
                    throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null);
            }
        }
    }
}
