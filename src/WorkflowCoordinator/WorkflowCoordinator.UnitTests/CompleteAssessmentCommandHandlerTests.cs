using System;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using NUnit.Framework;
using WorkflowCoordinator.Handlers;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.UnitTests
{
    public class CompleteAssessmentCommandHandlerTests
    {
        private TestableMessageHandlerContext _handlerContext;
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private CompleteAssessmentCommandHandler _handler;
        private ILogger<CompleteAssessmentCommandHandler> _fakeLogger;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeLogger = A.Dummy<ILogger<CompleteAssessmentCommandHandler>>();

            _handlerContext = new TestableMessageHandlerContext();

            _handler = new CompleteAssessmentCommandHandler(_fakeLogger, _fakeDataServiceApiClient, _dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_Handle_Given_ProcessId_does_not_exists_Then_Throws_Exception()
        {
            //Given
            var fromActivity = WorkflowStage.Verify;
            var processId = 123;
            var workflowInstanceId = 1;

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = "VERIFY_SERIAL_NUMBER"
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var verifyData = new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };

            await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);
            await _dbContext.SaveChangesAsync();

            var completeAssessmentCommand = new CompleteAssessmentCommand()
            {
                CorrelationId = Guid.Empty,
                ProcessId = 456
            };

            //When
            Assert.ThrowsAsync<ApplicationException>(() =>
                _handler.Handle(completeAssessmentCommand, _handlerContext));
        }

        [TestCase("Simple", "Imm Act - NM")]
        [TestCase("LTA (Product only)", "Longer-term Action")]
        [TestCase("LTA", "Longer-term Action")]
        [Test]
        public async Task Test_Handle_Given_SdocId_Not_Assessed_For_ProcessId_Then_Action_Is_Generated_based_On_TaskType(string taskType, string action)
        {
            //Given
            var fromActivity = WorkflowStage.Verify;
            var processId = 123;
            var workflowInstanceId = 1;
            var sdocId = 1;

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = "VERIFY_SERIAL_NUMBER"
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var verifyData = new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                TaskType = taskType
            };
            await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);

            var primarydocument = new PrimaryDocumentStatus()
            {
                PrimaryDocumentStatusId = 1,
                ContentServiceId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                ProcessId = processId,
                SdocId = sdocId,
                StartedAt = DateTime.Today,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            };
            await _dbContext.PrimaryDocumentStatus.AddAsync(primarydocument);

            await _dbContext.SaveChangesAsync();

            var completeAssessmentCommand = new CompleteAssessmentCommand()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId
            };

            //When
            await _handler.Handle(completeAssessmentCommand, _handlerContext);

            //Then
            A.CallTo(() => _fakeDataServiceApiClient.MarkAssessmentAsAssessed(processId.ToString(), sdocId, action, "tbc")).MustHaveHappened();
            A.CallTo(() => _fakeDataServiceApiClient.MarkAssessmentAsCompleted(sdocId, "Marked Completed via TM2")).MustHaveHappened();

        }

        [Test]
        public async Task Test_Handle_Given_SdocId_Is_Assessed_For_ProcessId_Then_Only_A_Call_For_Marking_As_Completed_Is_Made_To_Sdra_Api()
        {
            //Given
            var fromActivity = WorkflowStage.Verify;
            var processId = 123;
            var workflowInstanceId = 1;
            var sdocId = 1;

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = "VERIFY_SERIAL_NUMBER"
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var verifyData = new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                TaskType = "Simple"
            };
            await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);

            var primarydocument = new PrimaryDocumentStatus()
            {
                PrimaryDocumentStatusId = 1,
                ContentServiceId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                ProcessId = processId,
                SdocId = sdocId,
                StartedAt = DateTime.Today,
                Status = SourceDocumentRetrievalStatus.Assessed.ToString()
            };
            await _dbContext.PrimaryDocumentStatus.AddAsync(primarydocument);

            await _dbContext.SaveChangesAsync();

            var completeAssessmentCommand = new CompleteAssessmentCommand()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId
            };

            //When
            await _handler.Handle(completeAssessmentCommand, _handlerContext);

            //Then
            A.CallTo(() => _fakeDataServiceApiClient.MarkAssessmentAsAssessed(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeDataServiceApiClient.MarkAssessmentAsCompleted(sdocId, "Marked Completed via TM2")).MustHaveHappened();

        }

        [Test]
        public async Task Test_Handle_Given_SdocId_Already_Marked_Completed_For_Another_ProcessId_Then_No_Call_Is_Made_To_Sdra_Api()
        {
            //Given
            var fromActivity = WorkflowStage.Verify;
            var processId = 123;
            var workflowInstanceId = 1;
            var sdocId = 1;

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = "VERIFY_SERIAL_NUMBER"
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var verifyData = new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                TaskType = "Simple"
            };
            await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);

            var primarydocument = new PrimaryDocumentStatus()
            {
                PrimaryDocumentStatusId = 1,
                ContentServiceId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                ProcessId = processId,
                SdocId = sdocId,
                StartedAt = DateTime.Today,
                Status = SourceDocumentRetrievalStatus.Completed.ToString()
            };
            await _dbContext.PrimaryDocumentStatus.AddAsync(primarydocument);

            await _dbContext.SaveChangesAsync();

            var completeAssessmentCommand = new CompleteAssessmentCommand()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId
            };

            //When
            await _handler.Handle(completeAssessmentCommand, _handlerContext);

            //Then
            A.CallTo(() => _fakeDataServiceApiClient.MarkAssessmentAsAssessed(A<string>.Ignored, A<int>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeDataServiceApiClient.MarkAssessmentAsCompleted(A<int>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

        }


    }
}
