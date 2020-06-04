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

namespace WorkflowCoordinator.UnitTests
{
    public class ProgressWorkflowInstanceEventHandlerTests
    {
        private TestableMessageHandlerContext _handlerContext;
        private ProgressWorkflowInstanceEventHandler _handler;
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private ILogger<ProgressWorkflowInstanceEventHandler> _fakeLogger;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakeLogger = A.Dummy<ILogger<ProgressWorkflowInstanceEventHandler>>();

            _handlerContext = new TestableMessageHandlerContext();

            _handler = new ProgressWorkflowInstanceEventHandler(_fakeWorkflowServiceApiClient, _fakeLogger);
        }

        [TestCase(WorkflowStage.Review, WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Assess, WorkflowStage.Verify)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Completed)]
        public async Task Test_Handle_ProgressWorkflowInstanceEvent_When_Progressing_and_K2_Task_Is_At_FromAction_Then_Task_Is_Progressed_In_K2_And_PersistWorkflowInstanceDataCommand_is_Fired(
                                                                                                                                        WorkflowStage fromAction,
                                                                                                                                        WorkflowStage toAction)
        {
            var k2SerialNumber = "234_123";
            var processId = 234;

            var progressWorkflowInstanceEvent = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromAction,
                ToActivity = toAction
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(progressWorkflowInstanceEvent.ProcessId))
                .Returns(new K2TaskData()
                {
                    ActivityName = fromAction.ToString(),
                    SerialNumber = k2SerialNumber,
                    WorkflowInstanceID = processId
                });

            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(k2SerialNumber))
                .Returns(true);

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
        public async Task Test_Handle_ProgressWorkflowInstanceEvent_When_Terminating_and_K2_Task_Returns_At_Review_Then_Task_Is_Terminated_In_K2_And_PersistWorkflowInstanceDataCommand_is_Fired()
        {
            var k2SerialNumber = "234_123";
            var processId = 234;
            var fromAction = WorkflowStage.Review;
            var toAction = WorkflowStage.Terminated;

            var progressWorkflowInstanceEvent = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromAction,
                ToActivity = toAction
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(progressWorkflowInstanceEvent.ProcessId))
                .Returns(new K2TaskData()
                {
                    ActivityName = fromAction.ToString(),
                    SerialNumber = k2SerialNumber,
                    WorkflowInstanceID = processId
                });

            //When
            await _handler.Handle(progressWorkflowInstanceEvent, _handlerContext);

            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.TerminateWorkflowInstance(k2SerialNumber))
    .MustHaveHappened();

            Assert.AreEqual(1, _handlerContext.SentMessages.Length);

            var persistWorkflowInstanceDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is PersistWorkflowInstanceDataCommand);
            Assert.IsNotNull(persistWorkflowInstanceDataCommand, $"No message of type {nameof(PersistWorkflowInstanceDataCommand)} seen.");
        }


        [Test]
        public async Task Test_Handle_ProgressWorkflowInstanceEvent_When_Rejecting_and_K2_Task_Returns_At_Verify_Then_Task_Is_Rejected_In_K2_And_PersistWorkflowInstanceDataCommand_is_Fired()
        {
            var k2SerialNumber = "234_123";
            var processId = 234;
            var fromAction = WorkflowStage.Verify;
            var toAction = WorkflowStage.Rejected;

            var progressWorkflowInstanceEvent = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromAction,
                ToActivity = toAction
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(progressWorkflowInstanceEvent.ProcessId))
                .Returns(new K2TaskData()
                {
                    ActivityName = fromAction.ToString(),
                    SerialNumber = k2SerialNumber,
                    WorkflowInstanceID = processId
                });

            A.CallTo(() => _fakeWorkflowServiceApiClient.RejectWorkflowInstance(k2SerialNumber))
                .Returns(true);

            //When
            await _handler.Handle(progressWorkflowInstanceEvent, _handlerContext);

            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.RejectWorkflowInstance(k2SerialNumber))
    .MustHaveHappened();

            Assert.AreEqual(1, _handlerContext.SentMessages.Length);

            var persistWorkflowInstanceDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is PersistWorkflowInstanceDataCommand);
            Assert.IsNotNull(persistWorkflowInstanceDataCommand, $"No message of type {nameof(PersistWorkflowInstanceDataCommand)} seen.");
        }

        [TestCase(WorkflowStage.Review, WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Assess, WorkflowStage.Verify)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Completed)]
        public void Test_Handle_ProgressWorkflowInstanceEvent_When_Progressing_Task_And_Task_Failed_To_Progress_In_K2_Then_ApplicationException_Is_Thrown(
                                                                                                                                                    WorkflowStage fromAction,
                                                                                                                                                    WorkflowStage toAction)
        {
            var k2SerialNumber = "234_123";
            var processId = 234;

            var message = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromAction,
                ToActivity = toAction
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId))
                .Returns(new K2TaskData()
                {
                    ActivityName = fromAction.ToString(),
                    SerialNumber = k2SerialNumber,
                    WorkflowInstanceID = processId
                });

            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(k2SerialNumber))
                .Returns(false);

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
        public void Test_Handle_ProgressWorkflowInstanceEvent_When_Terminating_Task_And_Task_Failed_To_Terminate_In_K2_Then_ApplicationException_Is_Thrown()
        {
            var k2SerialNumber = "234_123";
            var processId = 234;
            var fromAction = WorkflowStage.Review;
            var toAction = WorkflowStage.Terminated;

            var message = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromAction,
                ToActivity = toAction
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId))
                .Returns(new K2TaskData()
                {
                    ActivityName = fromAction.ToString(),
                    SerialNumber = k2SerialNumber,
                    WorkflowInstanceID = processId
                });

            A.CallTo(() => _fakeWorkflowServiceApiClient.TerminateWorkflowInstance(k2SerialNumber))
                .Throws<ApplicationException>();

            //When
            var ex = Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(message, _handlerContext));

            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.TerminateWorkflowInstance(k2SerialNumber))
                                    .MustHaveHappened();

            Assert.IsTrue(ex.Message.Contains($"Failed Terminating K2 task with SerialNumber: {k2SerialNumber}"));

            Assert.AreEqual(0, _handlerContext.SentMessages.Length);

            var persistWorkflowInstanceDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is PersistWorkflowInstanceDataCommand);
            Assert.IsNull(persistWorkflowInstanceDataCommand, $"Message of type {nameof(PersistWorkflowInstanceDataCommand)} seen.");
        }

        [Test]
        public void Test_Handle_ProgressWorkflowInstanceEvent_When_Rejecting_Task_And_Task_Failed_To_Reject_In_K2_Then_ApplicationException_Is_Thrown()
        {
            var k2SerialNumber = "234_123";
            var processId = 234;
            var fromAction = WorkflowStage.Verify;
            var toAction = WorkflowStage.Rejected;

            var message = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromAction,
                ToActivity = toAction
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId))
                .Returns(new K2TaskData()
                {
                    ActivityName = fromAction.ToString(),
                    SerialNumber = k2SerialNumber,
                    WorkflowInstanceID = processId
                });

            A.CallTo(() => _fakeWorkflowServiceApiClient.RejectWorkflowInstance(k2SerialNumber))
                .Returns(false);

            //When
            var ex = Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(message, _handlerContext));

            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.RejectWorkflowInstance(k2SerialNumber))
                                    .MustHaveHappened();

            Assert.AreEqual(
                $"Unable to reject task {processId} from Verify in K2.",
                    ex.Message);

            Assert.AreEqual(0, _handlerContext.SentMessages.Length);

            var persistWorkflowInstanceDataCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is PersistWorkflowInstanceDataCommand);
            Assert.IsNull(persistWorkflowInstanceDataCommand, $"Message of type {nameof(PersistWorkflowInstanceDataCommand)} seen.");
        }

        [TestCase(WorkflowStage.Review, WorkflowStage.Assess, WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Assess, WorkflowStage.Verify, WorkflowStage.Verify)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Completed, WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Review, WorkflowStage.Terminated, WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Rejected, WorkflowStage.Assess)]
        public void Test_Handle_ProgressWorkflowInstanceEvent_When_Task_In_K2_Is_At_Wrong_Activity_Then_ApplicationException_Is_Thrown(
                                                                                                                                            WorkflowStage fromAction,
                                                                                                                                            WorkflowStage toAction,
                                                                                                                                            WorkflowStage k2Action)
        {
            var processId = 234;

            var message = new ProgressWorkflowInstanceEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromAction,
                ToActivity = toAction
            };

            var k2Task = new K2TaskData()
            {
                ActivityName = k2Action.ToString(),
                SerialNumber = "234_123",
                WorkflowInstanceID = processId
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(message.ProcessId))
                .Returns(k2Task);

            //When
            var ex = Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(message, _handlerContext));

            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(A<string>.Ignored)).WithAnyArguments().MustNotHaveHappened();
            A.CallTo(() => _fakeWorkflowServiceApiClient.RejectWorkflowInstance(A<string>.Ignored)).WithAnyArguments().MustNotHaveHappened();
            A.CallTo(() => _fakeWorkflowServiceApiClient.TerminateWorkflowInstance(A<string>.Ignored)).WithAnyArguments().MustNotHaveHappened();

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
