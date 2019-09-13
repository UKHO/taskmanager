using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using Common.Messages.Commands;
using DataServices.Models;
using FakeItEasy;
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

namespace SourceDocumentCoordinator.UnitTests
{
    public class SourceDocumentRetrievalSagaTests
    {
        private TestableMessageHandlerContext _handlerContext;
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private WorkflowDbContext _dbContext;
        private SourceDocumentRetrievalSaga _sourceDocumentRetrievalSaga;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;
            _dbContext = new WorkflowDbContext(dbContextOptions);


            _handlerContext = new TestableMessageHandlerContext();

            var generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();

            _sourceDocumentRetrievalSaga = new SourceDocumentRetrievalSaga(_dbContext,
                _fakeDataServiceApiClient,
                generalConfig);

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
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored,A<bool>.Ignored)).Returns(new ReturnCode() { Code = 0 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalCommand, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _sourceDocumentRetrievalSaga.Data.CorrelationId);
            Assert.AreEqual(sdocId, _sourceDocumentRetrievalSaga.Data.SourceDocumentId);
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

        private UriConfig GetUriConfigs(IConfigurationRoot appConfigurationConfigRoot)
        {
            var uriConfig = new UriConfig();

            appConfigurationConfigRoot.GetSection("urls").Bind(uriConfig);

            return uriConfig;

        }
    }
}