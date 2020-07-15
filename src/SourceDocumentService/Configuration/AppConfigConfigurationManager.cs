using System.Configuration;

namespace SourceDocumentService.Configuration
{
    public class AppConfigConfigurationManager : IConfigurationManager
    {
        public string GetAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
