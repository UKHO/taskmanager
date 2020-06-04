using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.IntegrationTests
{
    public class K2DbAssessmentTests
    {
        private WorkflowServiceApiClient _workflowServiceApiClient;

        [SetUp]
        public void Setup()
        {
            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultConfigRoot = AzureKeyVaultConfigConfigurationRoot.Instance;

            var generalConfig = GetGeneralConfigs(appConfigurationConfigRoot);
            var uriConfig = GetUriConfigs(appConfigurationConfigRoot);
            var startupSecretsConfig = GetSecretsConfigs(keyVaultConfigRoot);
            
            IOptionsSnapshot<GeneralConfig> generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(generalConfig);
            IOptionsSnapshot<UriConfig> uriConfigOptions = new OptionsSnapshotWrapper<UriConfig>(uriConfig);

            _workflowServiceApiClient = SetupWorkflowServiceApiClient(startupSecretsConfig, generalConfigOptions, uriConfigOptions);
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Saves_Saga_Data()
        {
            // Given

            //When
            var isConnected = await _workflowServiceApiClient.CheckK2Connection();


            //Then
            Assert.IsTrue(isConnected);
        }

        private WorkflowDbContext WorkflowDbContext(SqlConnection connection)
        {
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();

            dbContextOptionsBuilder.UseSqlServer(connection);
            var dbContext = new WorkflowDbContext(dbContextOptionsBuilder.Options);

            return dbContext;
        }

        private SqlConnection SetupWorkflowDatabaseConnection(string workflowDbConnectionString, bool isLocalDebugging, StartupConfig startupConfig)
        {
            return new SqlConnection(workflowDbConnectionString)
            {
                AccessToken = isLocalDebugging ?
                    null :
                    new AzureServiceTokenProvider().GetAccessTokenAsync(startupConfig.AzureDbTokenUrl.ToString()).Result
            };
        }

        private WorkflowServiceApiClient SetupWorkflowServiceApiClient(StartupSecretsConfig startupSecretsConfig,
            IOptions<GeneralConfig> generalConfigOptions, IOptions<UriConfig> uriConfig)
        {
            return new WorkflowServiceApiClient(
                new HttpClient(
                    new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                        Credentials = new NetworkCredential(startupSecretsConfig.K2RestApiUsername, startupSecretsConfig.K2RestApiPassword)
                    }
                ), generalConfigOptions, uriConfig);
        }

        private StartupSecretsConfig GetSecretsConfigs(IConfigurationRoot keyVaultConfigRoot)
        {
            var startupSecretsConfig = new StartupSecretsConfig();

            keyVaultConfigRoot.GetSection("K2RestApi").Bind(startupSecretsConfig);

            return startupSecretsConfig;
        }

        private GeneralConfig GetGeneralConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var generalConfig = new GeneralConfig();

            appConfigurationConfigRoot.GetSection("k2").Bind(generalConfig);
            appConfigurationConfigRoot.GetSection("apis").Bind(generalConfig);

            return generalConfig;

        }

        private UriConfig GetUriConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var uriConfig = new UriConfig();

            appConfigurationConfigRoot.GetSection("urls").Bind(uriConfig);

            return uriConfig;

        }

        private StartupConfig GetStartupConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var startupConfig = new StartupConfig();

            appConfigurationConfigRoot.GetSection("databases").Bind(startupConfig);
            appConfigurationConfigRoot.GetSection("nsb").Bind(startupConfig);
            appConfigurationConfigRoot.GetSection("urls").Bind(startupConfig);

            return startupConfig;
        }
    }
}