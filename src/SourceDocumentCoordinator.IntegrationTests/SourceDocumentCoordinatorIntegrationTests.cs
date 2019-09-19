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
        private OptionsSnapshotWrapper<SecretsConfig> _secretsConfigWrapper;

        [SetUp]
        public void SetUp()
        {
            var httpClient = new HttpClient();

            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultConfigRoot = AzureKeyVaultConfigConfigurationRoot.Instance;

            var generalConfig = GetGeneralConfigs(appConfigurationConfigRoot);
            var uriConfig = GetUriConfigs(appConfigurationConfigRoot);
            var secretsConfig = GetSecretsConfigs(keyVaultConfigRoot);

            var generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(generalConfig);

            _uriConfigWrapper = new OptionsSnapshotWrapper<UriConfig>(uriConfig);
            _secretsConfigWrapper = new OptionsSnapshotWrapper<SecretsConfig>(secretsConfig);

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
                Credentials = new NetworkCredential(_secretsConfigWrapper.Value.ContentServiceUsername,
                    _secretsConfigWrapper.Value.ContentServicePassword,
                    _secretsConfigWrapper.Value.ContentServiceDomain)
            };

            var httpClient = new HttpClient(hndlr);
            _contentServiceApiClient = new ContentServiceApiClient(httpClient, _uriConfigWrapper, _secretsConfigWrapper);

            await _contentServiceApiClient.Post(System.IO.File.ReadAllBytes("C:\\temp\\2.pdf"), "2.pdf");
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

        private SecretsConfig GetSecretsConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var secretsConfig = new SecretsConfig();

            appConfigurationConfigRoot.GetSection("contentservice").Bind(secretsConfig);

            return secretsConfig;
        }
    }
}
