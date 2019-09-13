using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;

namespace SourceDocumentCoordinator.IntegrationTests
{
    public class SourceDocumentCoordinatorIntegrationTests
    {
        private IDataServiceApiClient _dataServiceApiClient;

        [SetUp]
        public void SetUp()
        {
            var httpClient = new HttpClient();

            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;

            var generalConfig = GetGeneralConfigs(appConfigurationConfigRoot);
            var uriConfig = GetUriConfigs(appConfigurationConfigRoot);

            var generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(generalConfig);

            var uriOptions = new OptionsSnapshotWrapper<UriConfig>(uriConfig);

            _dataServiceApiClient = new DataServiceApiClient(httpClient, generalConfigOptions, uriOptions);
        }

        [Test]
        public async Task Test_DataServiceApi_Connectivity()
        {
            Assert.AreEqual(await _dataServiceApiClient.CheckDataServicesConnection(), true);
        }

        private GeneralConfig GetGeneralConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var generalConfig = new GeneralConfig();

            appConfigurationConfigRoot.GetSection("apis").Bind(generalConfig);

            return generalConfig;
        }

        private UriConfig GetUriConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var uriConfig = new UriConfig();

            appConfigurationConfigRoot.GetSection("urls").Bind(uriConfig);

            return uriConfig;
        }
    }
}
