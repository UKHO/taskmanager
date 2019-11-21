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
        public IDocumentStatusProcessor GetDocumentStatusProcessor(SourceDocumentType documentType)
        {
            switch (documentType)
            {
                case SourceDocumentType.Primary:
                    return new PrimaryDocumentStatusProcessor(_dbContext);
                case SourceDocumentType.Linked:
                    return new LinkedDocumentStatusProcessor(_dbContext);
                case SourceDocumentType.Database:
                    return new DatabaseDocumentStatusProcessor(_dbContext);
                case SourceDocumentType.Folder:
                    throw new NotImplementedException("Document type not implemented.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(documentType), documentType, null);
            }
        }
    }
}
