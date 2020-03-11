using Microsoft.Extensions.Options;
using Portal.Configuration;

namespace Portal.Helpers
{
    public class CarisProjectNameGenerator : ICarisProjectNameGenerator
    {
        private readonly IOptions<GeneralConfig> _generalConfig;

        public CarisProjectNameGenerator(IOptions<GeneralConfig> generalConfig)
        {
            _generalConfig = generalConfig;
        }

        public string Generate(int processId, string parsedRsdraNumber, string sourceDocumentName)
        {
            var projectName = $"{processId}_{parsedRsdraNumber}_{sourceDocumentName}";

            if (projectName.Length > _generalConfig.Value.CarisProjectNameCharacterLimit)
            {
                projectName = projectName.Substring(0, _generalConfig.Value.CarisProjectNameCharacterLimit).TrimEnd();
            }

            return projectName;
        }
    }
}