namespace Portal.Helpers
{
    public interface ICarisProjectNameGenerator
    {
        string Generate(int processId, string parsedRsdraNumber, string sourceDocumentName);
    }
}