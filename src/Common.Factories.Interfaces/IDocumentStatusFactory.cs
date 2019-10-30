using Common.Messages.Enums;

namespace Common.Factories.Interfaces
{
    public interface IDocumentStatusFactory
    {
        IDocumentStatusProcessor GetDocumentStatusProcessor(SourceDocumentType documentType);
    }
}
