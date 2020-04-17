using Common.Messages.Enums;

namespace Common.Factories.Interfaces
{
    public interface IDocumentFileLocationFactory
    {
        IDocumentFileLocationProcessor GetDocumentFileLocationProcessor(SourceType sourceType);
    }
}
