using System.Net;
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
        private IContentServiceApiClient _contentServiceApiClient;
        private OptionsSnapshotWrapper<UriConfig> _uriConfigWrapper;

        [SetUp]
        public void SetUp()
        {
            var httpClient = new HttpClient();

            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;

            var generalConfig = GetGeneralConfigs(appConfigurationConfigRoot);
            var uriConfig = GetUriConfigs(appConfigurationConfigRoot);

            var generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(generalConfig);

            _uriConfigWrapper = new OptionsSnapshotWrapper<UriConfig>(uriConfig);

            _dataServiceApiClient = new DataServiceApiClient(httpClient, generalConfigOptions, _uriConfigWrapper);
        }

        [Test]
        public async Task Test_DataServiceApi_Connectivity()
        {
            Assert.AreEqual(await _dataServiceApiClient.CheckDataServicesConnection(), true);
        }

        [Test]
        public async Task Test_ContentService_Connectivity()
        {
            var hndlr = new HttpClientHandler()
            {
                //Credentials = new NetworkCredential("tbc", "tbc"),
                UseDefaultCredentials = true
            };
            
            var httpClient = new HttpClient(hndlr);
            _contentServiceApiClient = new ContentServiceApiClient(httpClient, _uriConfigWrapper);

            await _contentServiceApiClient.Post();
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
