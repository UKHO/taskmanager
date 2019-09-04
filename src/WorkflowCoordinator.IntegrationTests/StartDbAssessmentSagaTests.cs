using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NServiceBus.Testing;
using NUnit.Framework;
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
        private WorkflowDbContext _dbContext;
        private GeneralConfig _generalConfig;

        [SetUp]
        public void Setup()
        {
            _generalConfig = new GeneralConfig();
            var startupSecretsConfig = new StartupSecretsConfig();

            var appConfigurationConfigRoot = AzureAppConfigConfigurationRoot.Instance;
            appConfigurationConfigRoot.GetSection("k2").Bind(_generalConfig);
            appConfigurationConfigRoot.GetSection("apis").Bind(_generalConfig);
            appConfigurationConfigRoot.GetSection("urls").Bind(_generalConfig);

            var keyVaultConfigRoot = AzureKeyVaultConfigConfigurationRoot.Instance;
            keyVaultConfigRoot.GetSection("K2RestApi").Bind(startupSecretsConfig);


            IOptionsSnapshot<GeneralConfig> generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(_generalConfig);

            _handlerContext = new TestableMessageHandlerContext();

            var dataServiceApiClient = new DataServiceApiClient(new HttpClient(), generalConfigOptions);
            var workflowServiceApiClient = new WorkflowServiceApiClient(
                new HttpClient(
                    new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true,
                        Credentials = new NetworkCredential(startupSecretsConfig.NsbToK2ApiUsername, startupSecretsConfig.NsbToK2ApiPassword)
                    }
                ), generalConfigOptions);

            //TODO: Setup dbcontext

            _startDbAssessmentSaga = new StartDbAssessmentSaga(generalConfigOptions, dataServiceApiClient, workflowServiceApiClient, _dbContext);
        }

        [Test]
        public async Task Test1()
        {
            // Given

            var correlationId = Guid.NewGuid();
            var sourceDocumentId = 12345678;
            var startDbAssessmentCommand = new StartDbAssessmentCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sourceDocumentId
            };
            _startDbAssessmentSaga.Data = new StartDbAssessmentSagaData()
            {

            };

            //When
            await _startDbAssessmentSaga.Handle(startDbAssessmentCommand, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _startDbAssessmentSaga.Data.CorrelationId);
            Assert.AreEqual(sourceDocumentId, _startDbAssessmentSaga.Data.SourceDocumentId);
        }
    }
}