using Common.Helpers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using NServiceBus.Testing;

using NUnit.Framework;

using System;
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
        private WorkflowDbContext _dbContext;
        private GeneralConfig _generalConfig;

        [SetUp]
        public void Setup()
        { 
            var configurationRoot = AzureAppConfigConfigurationRoot.Instance;

            _generalConfig = new GeneralConfig();
            configurationRoot.GetSection("k2").Bind(_generalConfig);
            configurationRoot.GetSection("apis").Bind(_generalConfig);


            IOptionsSnapshot<GeneralConfig> generalConfigOptions = new OptionsSnapshotWrapper<GeneralConfig>(_generalConfig);

            _handlerContext = new TestableMessageHandlerContext();

            var dataServiceApiClient = new DataServiceApiClient(new HttpClient(), generalConfigOptions);
            var workflowServiceApiClient = new WorkflowServiceApiClient(new HttpClient(), generalConfigOptions);

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

            //When
            await _startDbAssessmentSaga.Handle(startDbAssessmentCommand, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _startDbAssessmentSaga.Data.CorrelationId);
            Assert.AreEqual(sourceDocumentId, _startDbAssessmentSaga.Data.SourceDocumentId);
        }
    }
}