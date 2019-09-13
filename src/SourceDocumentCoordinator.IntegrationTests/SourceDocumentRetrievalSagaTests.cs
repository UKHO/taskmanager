using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NServiceBus.Testing;
using NUnit.Framework;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Sagas;
using WorkflowDatabase.EF;

namespace SourceDocumentCoordinator.IntegrationTests
{
    public class SourceDocumentRetrievalSagaTests
    {
        private TestableMessageHandlerContext _handlerContext;
        private IDataServiceApiClient _dataServiceApiClient;
        private WorkflowDbContext _dbContext;
        private SourceDocumentRetrievalSaga _sourceDocumentRetrievalSaga;
        private IOptionsSnapshot<GeneralConfig> _generalConfigOptions;

        [SetUp]
        public void SetUp()
        {
            _handlerContext = new TestableMessageHandlerContext();
            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            var keyVaultConfigRoot = AzureKeyVaultConfigConfigurationRoot.Instance;

            var startupConfig = GetStartupConfigs(appConfigurationConfigRoot);
            var generalConfig = GetGeneralConfigs(appConfigurationConfigRoot);

            _generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(generalConfig);

            var isLocalDebugging = ConfigHelpers.IsLocalDevelopment;

            var workflowDbConnectionString = DatabasesHelpers.BuildSqlConnectionString(isLocalDebugging,
                isLocalDebugging ? startupConfig.LocalDbServer : startupConfig.WorkflowDbServer, startupConfig.WorkflowDbName);

            var connection = SetupWorkflowDatabaseConnection(workflowDbConnectionString, isLocalDebugging, startupConfig);

            _dbContext = WorkflowDbContext(connection);

            _sourceDocumentRetrievalSaga = new SourceDocumentRetrievalSaga(_dbContext, 
                _dataServiceApiClient, 
                _generalConfigOptions);
        }

    [Test]
        public async Task Test_SourceDocumentRetrievalSaga_Saves_Saga_Data()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();
            var initiateSourceDocumentRetrievalCommand = new InitiateSourceDocumentRetrievalCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId,
                ProcessId = 1
            };
            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalCommand, _handlerContext);
            _sourceDocumentId = _sourceDocumentRetrievalSaga.Data.SourceDocumentId;

            //Then
            Assert.AreEqual(correlationId, _sourceDocumentRetrievalSaga.Data.CorrelationId);
            Assert.AreEqual(sdocId, _sourceDocumentRetrievalSaga.Data.SourceDocumentId);
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

        private StartupConfig GetStartupConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var startupConfig = new StartupConfig();

            appConfigurationConfigRoot.GetSection("databases").Bind(startupConfig);
            appConfigurationConfigRoot.GetSection("nsb").Bind(startupConfig);
            appConfigurationConfigRoot.GetSection("urls").Bind(startupConfig);

            return startupConfig;
        }

        private GeneralConfig GetGeneralConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var generalConfig = new GeneralConfig();

            appConfigurationConfigRoot.GetSection("apis").Bind(generalConfig);

            return generalConfig;
        }
    }
}
