using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
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
using SourceDocumentCoordinator.Enums;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;
using SourceDocumentCoordinator.Sagas;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

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
        public async Task Test_SourceDocumentRetrievalSaga_saves_saga_data_when_return_code_is_0()
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
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 0 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalCommand, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _sourceDocumentRetrievalSaga.Data.CorrelationId);
            Assert.AreEqual(sdocId, _sourceDocumentRetrievalSaga.Data.SourceDocumentId);
        }

        [Test]
        public async Task Test_SourceDocumentRetrievalSaga_saves_saga_data_and_source_doc_status_when_return_code_is_1_and_source_document_status_is_0()
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
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 1 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalCommand, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _sourceDocumentRetrievalSaga.Data.CorrelationId);
            Assert.AreEqual(sdocId, _sourceDocumentRetrievalSaga.Data.SourceDocumentId);
            Assert.AreEqual(_dbContext.SourceDocumentStatus.Any(), true);
        }

        [Test]
        public async Task Test_SourceDocumentRetrievalSaga_does_not_save_source_doc_status_when_return_code_is_1_and_source_document_status_is_1()
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
            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData
            {
                CorrelationId = correlationId,
                SourceDocumentStatusId = 1,
                IsStarted = true
            };
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 1 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalCommand, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _sourceDocumentRetrievalSaga.Data.CorrelationId);
            Assert.AreEqual(_dbContext.SourceDocumentStatus.Any(), false);
        }

        [Test]
        public async Task Test_SourceDocumentRetrievalSaga_requests_timeout()
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
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 0 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalCommand, _handlerContext);

            //Then
            var getDocumentRequestQueueStatusCommand = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                t.Message is GetDocumentRequestQueueStatusCommand);
            Assert.IsNotNull(getDocumentRequestQueueStatusCommand, $"No timeout of type {nameof(GetDocumentRequestQueueStatusCommand)}");
        }

        [Test]
        public async Task Test_SourceDocumentRetrievalSaga_When_Failed_Queuing_Does_not_fire_GetDocumentRequestQueueStatusCommand(
                        [Values(
                                QueueForRetrievalReturnCodeEnum.QueueInsertionFailed,
                                QueueForRetrievalReturnCodeEnum.SdocIdNotRecognised)]
                        QueueForRetrievalReturnCodeEnum returnCode)
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();
            var initiateSourceDocumentRetrievalCommand = new InitiateSourceDocumentRetrievalCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId,
                ProcessId = 1,
                GeoReferenced = true
            };
            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(
                                                                                A<string>.Ignored, 
                                                                                A<int>.Ignored, 
                                                                                A<string>.Ignored,
                                                                                A<bool>.Ignored))
                                            .Returns(new ReturnCode() {Message  = "Testing" , Code = (int)returnCode });

            //When

            Assert.ThrowsAsync<ApplicationException>(() => _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalCommand, _handlerContext));

            //Then
            var getDocumentRequestQueueStatusCommand = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                t.Message is GetDocumentRequestQueueStatusCommand);
            Assert.IsNull(getDocumentRequestQueueStatusCommand, $"Timeout '{nameof(GetDocumentRequestQueueStatusCommand)}' should only be fired on successful queuing");
        }


        [Test]
        public async Task Test_SourceDocumentRetrievalSaga_GetDocumentRequestQueueStatusCommand_Handler_Updates_DocumentRetrievalStatus_To_Ready()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();

            _dbContext.SourceDocumentStatus.Add(new SourceDocumentStatus()
            {
                ProcessId = 1,
                SdocId = sdocId,
                StartedAt = DateTime.Now,
                Status = SourceDocumentRetrievalStatus.Started.ToString()
            });
            _dbContext.SaveChanges();

            var getDocumentRequestQueueStatusCommand = new GetDocumentRequestQueueStatusCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId
            };

            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();

            //_sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentRequestQueueStatus(A<string>.Ignored)).Returns(new QueuedDocumentObjects()
            {
                new QueuedDocumentObject()
                {
                    SodcId = sdocId,
                    Code = 0
                }
            });

            //When
            await _sourceDocumentRetrievalSaga.Timeout(getDocumentRequestQueueStatusCommand, _handlerContext);

            //Then
            var sourceDocumentStatus =
                _dbContext.SourceDocumentStatus.FirstOrDefault(s => s.SdocId == sdocId);

            Assert.IsNotNull(sourceDocumentStatus, $"'{nameof(sourceDocumentStatus)}' should exists in SourceDocumentStatus table");
            Assert.IsTrue(sourceDocumentStatus.Status.Equals(SourceDocumentRetrievalStatus.Ready.ToString(), StringComparison.OrdinalIgnoreCase));

            Assert.IsTrue(_handlerContext.TimeoutMessages.Length == 0);
        }

        [Test]
        public async Task Test_SourceDocumentRetrievalSaga_GetDocumentRequestQueueStatusCommand_Handler_Fires_Another_Timeout_When_DocumentRetrievalStatus_Queued()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();

            var getDocumentRequestQueueStatusCommand = new GetDocumentRequestQueueStatusCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId
            };

            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();

            //_sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentRequestQueueStatus(A<string>.Ignored)).Returns(new QueuedDocumentObjects()
            {
                new QueuedDocumentObject()
                {
                    SodcId = sdocId,
                    Code = 1
                }
            });

            //When
            await _sourceDocumentRetrievalSaga.Timeout(getDocumentRequestQueueStatusCommand, _handlerContext);

            //Then
            Assert.IsTrue(_handlerContext.TimeoutMessages.Length > 0);
        }

        [Test]
        public async Task Test_Timeout_Handler_Sends_ClearDocumentRequestFromQueueCommand_And_PersistDocumentInStoreCommand_Messages()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();

            _dbContext.SourceDocumentStatus.Add(new SourceDocumentStatus()
            {
                ProcessId = 1,
                SdocId = sdocId,
                StartedAt = DateTime.Now,
                Status = SourceDocumentRetrievalStatus.Started.ToString()
            });
            _dbContext.SaveChanges();

            var getDocumentRequestQueueStatusCommand = new GetDocumentRequestQueueStatusCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId
            };

            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();

            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentRequestQueueStatus(A<string>.Ignored)).Returns(new QueuedDocumentObjects()
            {
                new QueuedDocumentObject()
                {
                    SodcId = sdocId,
                    Code = 0
                }
            });

            //When
            await _sourceDocumentRetrievalSaga.Timeout(getDocumentRequestQueueStatusCommand, _handlerContext);

            //Then
            Assert.AreEqual(2, _handlerContext.SentMessages.Length);

            var clearDocumentRequestFromQueueCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is ClearDocumentRequestFromQueueCommand);
            Assert.IsNotNull(clearDocumentRequestFromQueueCommand, $"No message of type {nameof(ClearDocumentRequestFromQueueCommand)} seen.");

            var persistDocumentInStoreCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is PersistDocumentInStoreCommand);
            Assert.IsNotNull(persistDocumentInStoreCommand, $"No message of type {nameof(PersistDocumentInStoreCommand)} seen.");
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