using AutoMapper;

using Common.Helpers;

using FakeItEasy;

using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using NServiceBus.Testing;

using NUnit.Framework;

using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Sagas;

using WorkflowDatabase.EF;

namespace WorkflowCoordinator.IntegrationTests
{
    public class StartDbAssessmentSagaTests
    {
        private StartDbAssessmentSaga _startDbAssessmentSaga;
        private TestableMessageHandlerContext _handlerContext;
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private WorkflowServiceApiClient _workflowServiceApiClient;
        private IMapper _fakeMapper;
        private int _processId;
        private int _sdocId;

        [SetUp]
        public void Setup()
        {
            _processId = 0;
            _sdocId = 0;
            _handlerContext = new TestableMessageHandlerContext();

            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultConfigRoot = AzureKeyVaultConfigConfigurationRoot.Instance;

            var generalConfig = GetGeneralConfigs(appConfigurationConfigRoot);
            var uriConfig = GetUriConfigs(appConfigurationConfigRoot);
            var startupConfig = GetStartupConfigs(appConfigurationConfigRoot);
            var startupSecretsConfig = GetSecretsConfigs(keyVaultConfigRoot);
            
            IOptionsSnapshot<GeneralConfig> generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(generalConfig);
            IOptionsSnapshot<UriConfig> uriConfigOptions = new OptionsSnapshotWrapper<UriConfig>(uriConfig);

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();

            _workflowServiceApiClient = SetupWorkflowServiceApiClient(startupSecretsConfig, generalConfigOptions, uriConfigOptions);

            var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging,
                isLocalDebugging ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName);

            var connection = SetupWorkflowDatabaseConnection(workflowDbConnectionString, isLocalDebugging, startupConfig);

            var dbContext = WorkflowDbContext(connection);
            _fakeMapper = A.Fake<IMapper>();

            _startDbAssessmentSaga = new StartDbAssessmentSaga(
                                                                generalConfigOptions, 
                                                                _fakeDataServiceApiClient, 
                                                                _workflowServiceApiClient, 
                                                                dbContext,
                                                                _fakeMapper);
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Saves_Saga_Data()
        {
            // Given
            _sdocId = 1111;
            var correlationId = Guid.NewGuid();
            var startDbAssessmentCommand = new StartDbAssessmentCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = _sdocId
            };
            _startDbAssessmentSaga.Data = new StartDbAssessmentSagaData();

            //When
            await _startDbAssessmentSaga.Handle(startDbAssessmentCommand, _handlerContext);
            _processId = _startDbAssessmentSaga.Data.ProcessId;

            //Then
            Assert.AreEqual(correlationId, _startDbAssessmentSaga.Data.CorrelationId);
            Assert.AreEqual(_sdocId, _startDbAssessmentSaga.Data.SourceDocumentId);
            Assert.IsTrue(_processId > 0);
        }

        [TearDown]
        public async Task CleanupTests()
        {
            if (_processId > 0)
            {
                var serialNumber = await _workflowServiceApiClient.GetWorkflowInstanceSerialNumber(_processId);
                await _workflowServiceApiClient.TerminateWorkflowInstance(serialNumber);


                //TODO: Remove WorkflowInstance record from WorkflowInstance table using _processId
            }
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