using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Messages.Commands;
using Common.Messages.Events;
using DataServices.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus.Testing;
using NUnit.Framework;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Sagas;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.UnitTests
{
    public class StartDbAssessmentSagaTests
    {
        private StartDbAssessmentSaga _saga;
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private TestableMessageHandlerContext _handlerContext;
        private WorkflowDbContext _dbContext;
        private IMapper _fakeMapper;
        private ILogger<StartDbAssessmentSaga> _fakeLogger;
        private IOptionsSnapshot<GeneralConfig> _fakeGeneralConfigOptionsSnapshot;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeGeneralConfigOptionsSnapshot = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            var generalConfig = new GeneralConfig { WorkflowCoordinatorAssessmentPollingIntervalSeconds = 5, CallerCode = "HDB" };
            A.CallTo(() => _fakeGeneralConfigOptionsSnapshot.Value).Returns(generalConfig);

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakeMapper = A.Fake<IMapper>();
            _fakeLogger = A.Dummy<ILogger<StartDbAssessmentSaga>>();
            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceSerialNumber(A<int>.Ignored)).Returns("1234");


            _saga = new StartDbAssessmentSaga(_fakeGeneralConfigOptionsSnapshot,
                _fakeDataServiceApiClient,
                _fakeWorkflowServiceApiClient,
                _dbContext,
                _fakeMapper,
                _fakeLogger)
            { Data = new StartDbAssessmentSagaData() };
            _handlerContext = new TestableMessageHandlerContext();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Saves_Saga_Data()
        {
            //Given
            var correlationId = Guid.NewGuid();
            var sourceDocumentId = 99;

            //When
            A.CallTo(()=> _fakeWorkflowServiceApiClient.GetWorkflowInstanceSerialNumber(A<int>.Ignored)).Returns("1234");
            await _saga.Handle(new StartDbAssessmentCommand
            {
                CorrelationId = correlationId,
                SourceDocumentId = sourceDocumentId
            }, _handlerContext);

            //Then
            Assert.AreEqual(correlationId, _saga.Data.CorrelationId);
            Assert.AreEqual(sourceDocumentId, _saga.Data.SourceDocumentId);
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_RetrieveAssessmentDataCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var retrieveAssessmentDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is RetrieveAssessmentDataCommand);
            Assert.IsNotNull(retrieveAssessmentDataCommand, $"No message of type {nameof(RetrieveAssessmentDataCommand)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_GetBackwardDocumentLinksCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var getBackwardDocumentLinksCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is GetBackwardDocumentLinksCommand);
            Assert.IsNotNull(getBackwardDocumentLinksCommand, $"No message of type {nameof(GetBackwardDocumentLinksCommand)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_GetForwardDocumentLinksCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var getForwardDocumentLinksCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is GetForwardDocumentLinksCommand);
            Assert.IsNotNull(getForwardDocumentLinksCommand, $"No message of type {nameof(GetForwardDocumentLinksCommand)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_GetSepDocumentLinksCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var getSepDocumentLinksCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is GetSepDocumentLinksCommand);
            Assert.IsNotNull(getSepDocumentLinksCommand, $"No message of type {nameof(GetSepDocumentLinksCommand)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Publishes_InitiateSourceDocumentRetrievalCommand()
        {
            //Given

            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            var initiateSourceDocumentRetrievalEvent = _handlerContext.PublishedMessages.SingleOrDefault(t =>
                t.Message is InitiateSourceDocumentRetrievalEvent);
            Assert.IsNotNull(initiateSourceDocumentRetrievalEvent, $"No message of type {nameof(InitiateSourceDocumentRetrievalEvent)} seen.");
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Sends_1_Messages()
        {
            //Given
            
            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            Assert.AreEqual(4, _handlerContext.SentMessages.Length);
        }

        [Test]
        public async Task Test_StartDbAssessmentCommand_Publishes_1_Messages()
        {
            //Given
            
            //When
            await _saga.Handle(A.Dummy<StartDbAssessmentCommand>(), _handlerContext);

            //Then
            Assert.AreEqual(1, _handlerContext.PublishedMessages.Length);
        }

        [Test]
        public async Task Test_RetrieveAssessmentDataCommand_Given_Valid_Data_Then_AssessmentData_Is_Added()
        {
            //Arrange
            var workflowInstanceId = 1;
            var processId = 2;
            var sourceDocumentId = 3;

            var retrieveAssessmentDataCommand = new RetrieveAssessmentDataCommand()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                CorrelationId = A.Dummy<Guid>(),
                SourceDocumentId = sourceDocumentId
            };

            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(A<string>.Ignored, sourceDocumentId))
                .Returns(Task.FromResult(new DocumentAssessmentData()
                {
                    SdocId = sourceDocumentId
                }));

            A.CallTo(() => _fakeMapper.Map<DocumentAssessmentData, AssessmentData>(A<DocumentAssessmentData>.Ignored))
                .Returns(new AssessmentData() {PrimarySdocId = sourceDocumentId} );

            //Act
            await _saga.Handle(retrieveAssessmentDataCommand, _handlerContext);

            //Assert
            var assessmentData = await _dbContext.AssessmentData.FirstOrDefaultAsync();
            Assert.IsNotNull(assessmentData);
            Assert.AreEqual(processId, assessmentData.ProcessId);
            Assert.AreEqual(sourceDocumentId, assessmentData.PrimarySdocId);
            Assert.IsFalse(_dbContext.ChangeTracker.HasChanges());
        }

        [Test]
        public async Task Test_RetrieveAssessmentDataCommand_Given_Valid_Data_Then_Saga_Is_Marked_Complete()
        {
            //Arrange
            var workflowInstanceId = 1;
            var processId = 2;
            var sourceDocumentId = 3;

            var retrieveAssessmentDataCommand = new RetrieveAssessmentDataCommand()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                CorrelationId = A.Dummy<Guid>(),
                SourceDocumentId = sourceDocumentId
            };

            A.CallTo(() => _fakeDataServiceApiClient.GetAssessmentData(A<string>.Ignored, sourceDocumentId))
                .Returns(Task.FromResult(new DocumentAssessmentData()
                {
                    SdocId = sourceDocumentId
                }));

            A.CallTo(() => _fakeMapper.Map<DocumentAssessmentData, AssessmentData>(A<DocumentAssessmentData>.Ignored))
                .Returns(new AssessmentData() {PrimarySdocId = sourceDocumentId} );

            //Act
            await _saga.Handle(retrieveAssessmentDataCommand, _handlerContext);

            //Assert
            Assert.IsTrue(_saga.Completed);
        }
    }
}