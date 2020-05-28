using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Events;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using NUnit.Framework;
using WorkflowCoordinator.Handlers;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.UnitTests
{
    public class ProgressWorkflowInstanceEventHandlerTests
    {
        private TestableMessageHandlerContext _handlerContext;
        private ProgressWorkflowInstanceEventHandler _handler;
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private ILogger<ProgressWorkflowInstanceEventHandler> _fakeLogger;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakeLogger = A.Dummy<ILogger<ProgressWorkflowInstanceEventHandler>>();

            _handlerContext = new TestableMessageHandlerContext();

            _handler = new ProgressWorkflowInstanceEventHandler(_fakeWorkflowServiceApiClient,
                _fakeLogger, _dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_Handle_ProgressWorkflowInstanceEvent_When_K2_Task_Returns_At_Assess_Then_Task_Is_Progressed_In_K2_And_Data_Is_Persisted()
        {
            var k2SerialNumber = "234_123";
            var processId = 234;
            var workflowInstanceId = 1;

            var progressWorkflowInstanceEvent = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = WorkflowStage.Assess,
                ToActivity = WorkflowStage.Verify
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(progressWorkflowInstanceEvent.ProcessId))
                .Returns(new K2TaskData()
                {
                    ActivityName = WorkflowStage.Assess.ToString(),
                    SerialNumber = k2SerialNumber,
                    WorkflowInstanceID = processId
                });

            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(k2SerialNumber))
                .Returns(true);

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = WorkflowStage.Assess.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = k2SerialNumber
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var assessData = new DbAssessmentAssessData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentAssessData.AddAsync(assessData);
            await _dbContext.SaveChangesAsync();

            //When
            await _handler.Handle(progressWorkflowInstanceEvent, _handlerContext);

            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(k2SerialNumber))
    .MustHaveHappened();

            Assert.AreEqual(1, _handlerContext.SentMessages.Length);

            var persistWorkflowInstanceDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is PersistWorkflowInstanceDataCommand);
            Assert.IsNotNull(persistWorkflowInstanceDataCommand, $"No message of type {nameof(PersistWorkflowInstanceDataCommand)} seen.");
        }

        [Test]
        public async Task Test_Handle_ProgressWorkflowInstanceEvent_When_Progressing_Task_To_Verify_And_Task_Failed_To_Progress_In_K2_Then_ApplicationException_Is_Thrown()
        {
            var k2SerialNumber = "234_123";
            var processId = 234;
            var workflowInstanceId = 1;

            var message = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = WorkflowStage.Assess,
                ToActivity = WorkflowStage.Verify
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId))
                .Returns(new K2TaskData()
                {
                    ActivityName = WorkflowStage.Assess.ToString(),
                    SerialNumber = k2SerialNumber,
                    WorkflowInstanceID = processId
                });

            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(k2SerialNumber))
                .Returns(false);

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = WorkflowStage.Assess.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = k2SerialNumber
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var assessData = new DbAssessmentAssessData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentAssessData.AddAsync(assessData);
            await _dbContext.SaveChangesAsync();

            //When
            var ex = Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(message, _handlerContext));
            
            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(k2SerialNumber))
                                    .MustHaveHappened();

            Assert.AreEqual(
                    $"Unable to progress task {message.ProcessId} from {message.FromActivity} to {message.ToActivity} in K2.",
                    ex.Message);

            Assert.AreEqual(0, _handlerContext.SentMessages.Length);

            var persistWorkflowInstanceDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is PersistWorkflowInstanceDataCommand);
            Assert.IsNull(persistWorkflowInstanceDataCommand, $"Message of type {nameof(PersistWorkflowInstanceDataCommand)} seen.");
        }

        [Test]
        public void Test_Handle_ProgressWorkflowInstanceEvent_When_Progressing_To_Assess_And_Task_Is_At_Review_Then_ApplicationException_Is_Thrown()
        {
            var processId = 234;

            var message = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = WorkflowStage.Assess,
                ToActivity = WorkflowStage.Verify
            };

            var k2Task = new K2TaskData()
            {
                ActivityName = WorkflowStage.Review.ToString(),
                SerialNumber = "234_123",
                WorkflowInstanceID = processId
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId))
                .Returns(k2Task);

            //When
            var ex = Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(message, _handlerContext));

            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(A<string>.Ignored)).WithAnyArguments().MustNotHaveHappened();

            Assert.AreEqual(
                $"Workflow instance with ProcessId {message.ProcessId} is not at the expected step {message.FromActivity} in K2 but was at {k2Task.ActivityName}," +
                $" while progressing task to {message.ToActivity}",
                ex.Message);

            Assert.AreEqual(0, _handlerContext.SentMessages.Length);

            var persistWorkflowInstanceDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                                                                                                                t.Message is PersistWorkflowInstanceDataCommand);
            Assert.IsNull(persistWorkflowInstanceDataCommand, $"Message of type {nameof(PersistWorkflowInstanceDataCommand)} seen.");
        }

    }
}
