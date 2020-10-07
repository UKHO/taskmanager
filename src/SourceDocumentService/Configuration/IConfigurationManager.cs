namespace SourceDocumentService.Configuration
{
    public interface IConfigurationManager
    {
        string GetAppSetting(string key);
        string GetConnectionString(string key);
    }
}
