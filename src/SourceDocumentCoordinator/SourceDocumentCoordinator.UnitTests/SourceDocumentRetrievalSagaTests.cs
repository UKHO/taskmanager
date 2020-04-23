using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.DocumentStatusFactory;
using Common.Messages.Enums;
using Common.Messages.Events;
using DataServices.Models;
using FakeItEasy;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private DocumentStatusFactory _documentStatusFactory;
        private ILogger<SourceDocumentRetrievalSaga> _fakeLogger;

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

            _documentStatusFactory = new DocumentStatusFactory(_dbContext);

            _fakeLogger = A.Dummy<ILogger<SourceDocumentRetrievalSaga>>();

            _sourceDocumentRetrievalSaga = new SourceDocumentRetrievalSaga(_dbContext,
                _fakeDataServiceApiClient,
                generalConfig,
                _documentStatusFactory,
                _fakeLogger);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_InitiateSourceDocumentRetrievalEvent_Saves_Saga_Data_When_Return_Code_Is_0()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();
            var initiateSourceDocumentRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId,
                ProcessId = 1
            };
            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 0 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalEvent, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _sourceDocumentRetrievalSaga.Data.CorrelationId);
            Assert.AreEqual(sdocId, _sourceDocumentRetrievalSaga.Data.SourceDocumentId);
        }

        [Test]
        public async Task Test_InitiateSourceDocumentRetrievalEvent_Handler_Uses_PrimaryDocumentStatusProcessor_to_create_a_PrimaryDocumentStatus_row()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();

            var initiateSourceDocumentRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId,
                ProcessId = 1
            };
            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 1 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalEvent, _handlerContext);

            Assert.IsNotNull(_dbContext.PrimaryDocumentStatus.First(r => r.ProcessId == 1 && r.SdocId == sdocId));
        }

        [Test]
        public async Task Test_InitiateSourceDocumentRetrievalEvent_Does_Not_Save_Source_Doc_Status_When_Return_Code_Is_1_And_Source_Document_Status_Is_1()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();
            var initiateSourceDocumentRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId,
                ProcessId = 1
            };
            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData
            {
                CorrelationId = correlationId,
                DocumentStatusId = 1,
                IsStarted = true
            };
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 1 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalEvent, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _sourceDocumentRetrievalSaga.Data.CorrelationId);
            Assert.AreEqual(_dbContext.PrimaryDocumentStatus.Any(), false);
        }

        [Test]
        public async Task Test_InitiateSourceDocumentRetrievalEvent_Requests_Timeout()
        {
            // Given
            var sourceDocumentId = 1111;
            var correlationId = Guid.NewGuid();
            var initiateSourceDocumentRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                SourceDocumentId = sourceDocumentId,
                ProcessId = 1,
                SourceType = SourceType.Primary
            };
            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData();
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 0 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalEvent, _handlerContext);

            //Then
            var getDocumentRequestQueueStatusCommand = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                t.Message is GetDocumentRequestQueueStatusCommand);
            Assert.IsNotNull(getDocumentRequestQueueStatusCommand, $"No timeout of type {nameof(GetDocumentRequestQueueStatusCommand)}");
        }

        [Test]
        public void Test_InitiateSourceDocumentRetrievalEvent_When_Failed_Queuing_Does_Not_Fire_GetDocumentRequestQueueStatusCommand(
                        [Values(
                                QueueForRetrievalReturnCodeEnum.QueueInsertionFailed,
                                QueueForRetrievalReturnCodeEnum.SdocIdNotRecognised)]
                        QueueForRetrievalReturnCodeEnum returnCode)
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();
            var initiateSourceDocumentRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
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
                                            .Returns(new ReturnCode() { Message = "Testing", Code = (int)returnCode });

            //When

            Assert.ThrowsAsync<ApplicationException>(() => _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalEvent, _handlerContext));

            //Then
            var getDocumentRequestQueueStatusCommand = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                t.Message is GetDocumentRequestQueueStatusCommand);
            Assert.IsNull(getDocumentRequestQueueStatusCommand, $"Timeout '{nameof(GetDocumentRequestQueueStatusCommand)}' should only be fired on successful queuing");
        }


        [Test]
        public async Task Test_GetDocumentRequestQueueStatusCommand_Updates_DocumentRetrievalStatus_To_Ready()
        {
            // Given
            var sdocId = 1111;
            var processId = 1;
            var correlationId = Guid.NewGuid();

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
            {
                ProcessId = processId,
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

            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData()
            {
                ProcessId = processId,
                SourceDocumentId = sdocId
            };

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
            var primaryDocumentStatus =
                _dbContext.PrimaryDocumentStatus.FirstOrDefault(s => s.SdocId == sdocId);

            Assert.IsNotNull(primaryDocumentStatus, $"'{nameof(primaryDocumentStatus)}' should exists in PrimaryDocumentStatus table");
            Assert.IsTrue(primaryDocumentStatus.Status.Equals(SourceDocumentRetrievalStatus.Ready.ToString(), StringComparison.OrdinalIgnoreCase));

            Assert.IsTrue(_handlerContext.TimeoutMessages.Length == 0);
        }

        [Test]
        public async Task Test_GetDocumentRequestQueueStatusCommand_When_Request_Queue_Succeeds_Does_Only_Fires_ClearDocumentRequestFromQueueCommand_and_PersistDocumentInStoreCommand(
                        [Values(
                            RequestQueueStatusReturnCodeEnum.Success,
                            RequestQueueStatusReturnCodeEnum.NotGeoreferenced)]
                        QueueForRetrievalReturnCodeEnum returnCode)
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
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentRequestQueueStatus(A<string>.Ignored))
                                            .Returns(new QueuedDocumentObjects() { new QueuedDocumentObject
                                            {
                                                Code = (int)returnCode,
                                                Message = "testing",
                                                StatusTime = DateTime.Now,
                                                SodcId = sdocId
                                            } });

            //When
            await _sourceDocumentRetrievalSaga.Timeout(getDocumentRequestQueueStatusCommand, _handlerContext);


            // Ensure new ClearDocumentRequestFromQueueCommand is sent
            var clearDocumentRequestFromQueueCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is ClearDocumentRequestFromQueueCommand);
            Assert.IsNotNull(clearDocumentRequestFromQueueCommand, $"No message of type {nameof(ClearDocumentRequestFromQueueCommand)} seen.");

            // Ensure new PersistDocumentInStoreCommand is sent
            var persistDocumentInStoreCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is PersistDocumentInStoreCommand);
            Assert.IsNotNull(persistDocumentInStoreCommand, $"No message of type {nameof(PersistDocumentInStoreCommand)} seen.");

            // Ensure no timeout is fired
            var timeoutCommand = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                t.Message is GetDocumentRequestQueueStatusCommand);
            Assert.IsNull(timeoutCommand, $"Timeout '{nameof(GetDocumentRequestQueueStatusCommand)}' should only be fired on successful queuing");

            // Ensure new InitiateSourceDocumentRetrievalEvent is not sent
            var initiateSourceDocumentRetrievalCommand = _handlerContext.PublishedMessages.SingleOrDefault(t =>
                t.Message is InitiateSourceDocumentRetrievalEvent);
            Assert.IsNull(initiateSourceDocumentRetrievalCommand, $"No message of type {nameof(InitiateSourceDocumentRetrievalEvent)} seen.");
        }

        [Test]
        public async Task Test_GetDocumentRequestQueueStatusCommand_Fires_Another_Timeout_When_DocumentRetrievalStatus_Queued()
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
                    Code = (int)RequestQueueStatusReturnCodeEnum.Queued
                }
            });

            //When
            await _sourceDocumentRetrievalSaga.Timeout(getDocumentRequestQueueStatusCommand, _handlerContext);

            //Then
            Assert.IsTrue(_handlerContext.TimeoutMessages.Length > 0);
        }

        [Test]
        public async Task Test_GetDocumentRequestQueueStatusCommand_When_Request_Queue_Status_Returns_Error_Code_Does_Not_Fire_Timeout_And_InitiateSourceDocumentRetrievalCommand_Is_Sent_With_GeoReferenced_Set_to_False(
                        [Values(
                            RequestQueueStatusReturnCodeEnum.ConversionFailed,
                            RequestQueueStatusReturnCodeEnum.NotSuitableForConversion,
            RequestQueueStatusReturnCodeEnum.ConversionTimeOut)]
                        QueueForRetrievalReturnCodeEnum returnCode)
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
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentRequestQueueStatus(A<string>.Ignored))
                                            .Returns(new QueuedDocumentObjects() { new QueuedDocumentObject
                                            {
                                                Code = (int)returnCode,
                                                Message = "testing",
                                                StatusTime = DateTime.Now,
                                                SodcId = sdocId
                                            } });

            //When
            await _sourceDocumentRetrievalSaga.Timeout(getDocumentRequestQueueStatusCommand, _handlerContext);

            // Then no timeout is fired in retry scenarios
            var timeoutCommand = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                t.Message is GetDocumentRequestQueueStatusCommand);
            Assert.IsNull(timeoutCommand, $"Timeout '{nameof(GetDocumentRequestQueueStatusCommand)}' should only be fired on successful queuing");

            // Ensure new InitiateSourceDocumentRetrievalEvent is sent
            var initiateSourceDocumentRetrievalCommand = _handlerContext.PublishedMessages.SingleOrDefault(t =>
                t.Message is InitiateSourceDocumentRetrievalEvent);
            Assert.IsNotNull(initiateSourceDocumentRetrievalCommand, $"No message of type {nameof(InitiateSourceDocumentRetrievalEvent)} seen.");

            Assert.IsFalse(((InitiateSourceDocumentRetrievalEvent)initiateSourceDocumentRetrievalCommand.Message).GeoReferenced);
        }

        [Test]
        public void Test_GetDocumentRequestQueueStatusCommand_When_Check_Queue_Status_Returns_Error_Throws_ApplicationException(
                        [Values(
                            RequestQueueStatusReturnCodeEnum.FolderNotWritable,
                            RequestQueueStatusReturnCodeEnum.QueueInsertionFailed)]
                        RequestQueueStatusReturnCodeEnum returnCode)
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

            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentRequestQueueStatus(A<string>.Ignored))
                .Returns(new QueuedDocumentObjects() { new QueuedDocumentObject
                {
                    Code = (int)returnCode,
                    Message = "testing",
                    StatusTime = DateTime.Now,
                    SodcId = sdocId
                } });

            Assert.ThrowsAsync<ApplicationException>(() => _sourceDocumentRetrievalSaga.Timeout(getDocumentRequestQueueStatusCommand, _handlerContext));
        }

        [Test]
        public async Task Test_GetDocumentRequestQueueStatusCommand_Sends_ClearDocumentRequestFromQueueCommand_And_PersistDocumentInStoreCommand_Messages()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();

            _dbContext.PrimaryDocumentStatus.Add(new PrimaryDocumentStatus()
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

        [Test]
        public async Task Test_GetDocumentRequestQueueStatusCommand_Creates_PrimaryDocumentStatus_Row_with_CorrelationId()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();

            var getDocumentRequestQueueStatusCommand = new GetDocumentRequestQueueStatusCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId,
                SourceType = SourceType.Primary
            };

            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData
            {
                CorrelationId = correlationId,
                DocumentStatusId = 1,
                IsStarted = true,
                SourceDocumentId = sdocId
            };

            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentRequestQueueStatus(A<string>.Ignored)).Returns(
                new QueuedDocumentObjects()
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
            var row = await _dbContext.PrimaryDocumentStatus.FirstAsync(pds => pds.SdocId == sdocId);
            Assert.AreEqual(correlationId, row.CorrelationId);
        }

        [Test]
        public async Task Test_InitiateSourceDocumentRetrievalEvent_Creates_PrimaryDocumentStatus_Row_with_CorrelationId()
        {
            // Given
            var sdocId = 1111;
            var correlationId = Guid.NewGuid();
            var initiateSourceDocumentRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                SourceDocumentId = sdocId,
                ProcessId = 1
            };
            _sourceDocumentRetrievalSaga.Data = new SourceDocumentRetrievalSagaData
            {
                CorrelationId = correlationId,
                DocumentStatusId = 1,
                IsStarted = true
            };
            A.CallTo(() => _fakeDataServiceApiClient.GetDocumentForViewing(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Returns(new ReturnCode() { Code = 0 });

            //When
            await _sourceDocumentRetrievalSaga.Handle(initiateSourceDocumentRetrievalEvent, _handlerContext);

            //Then
            var row = await _dbContext.PrimaryDocumentStatus.FirstAsync(pds => pds.SdocId == sdocId);
            Assert.AreEqual(correlationId, row.CorrelationId);
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