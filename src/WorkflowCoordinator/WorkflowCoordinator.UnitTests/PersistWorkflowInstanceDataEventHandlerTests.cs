using System;
using System.Collections.Generic;
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
using WorkflowCoordinator.Models;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.UnitTests
{
    public class PersistWorkflowInstanceDataEventHandlerTests
    {
        private TestableMessageHandlerContext _handlerContext;
        private PersistWorkflowInstanceDataEventHandler _handler;
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private ILogger<PersistWorkflowInstanceDataEventHandler> _fakeLogger;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakeLogger = A.Dummy<ILogger<PersistWorkflowInstanceDataEventHandler>>();

            _handlerContext = new TestableMessageHandlerContext();

            _handler = new PersistWorkflowInstanceDataEventHandler(_fakeWorkflowServiceApiClient,
                _fakeLogger, _dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_Handle_Given_FromActivity_Review_And_ToActivity_Assess_Then_WorkflowInstance_Data_Is_Updated()
        {
            //Given
            var fromActivity = WorkflowStage.Review;
            var toActivity = WorkflowStage.Assess;
            var processId = 1;
            var workflowInstanceId = 1;
            var currentSerialNumber = "ASSESS_SERIAL_NUMBER";

            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromActivity,
                ToActivity = toActivity,
            };

            var k2TaskData = new K2TaskData()
            {
                ActivityName = toActivity.ToString(),
                SerialNumber = currentSerialNumber
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns(Task.FromResult(k2TaskData));

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = "REVIEW_SERIAL_NUMBER"
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var reviewData = new DbAssessmentReviewData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentReviewData.AddAsync(reviewData);
            await _dbContext.SaveChangesAsync();

            //When
            await _handler.Handle(persistWorkflowInstanceDataEvent, _handlerContext);

            //Then
            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.WorkflowInstanceId == workflowInstanceId);
            Assert.AreEqual(currentSerialNumber,
                workflowInstance.SerialNumber);
            Assert.AreEqual(toActivity.ToString(),
                workflowInstance.ActivityName);
            Assert.AreEqual(WorkflowStatus.Started.ToString(),
                workflowInstance.Status);
        }

        [Test]
        public async Task Test_Handle_Given_FromActivity_Assess_And_ToActivity_Verify_Then_WorkflowInstance_Data_Is_Updated()
        {
            //Given
            var fromActivity = WorkflowStage.Assess;
            var toActivity = WorkflowStage.Verify;
            var processId = 1;
            var workflowInstanceId = 1;
            var currentSerialNumber = "VERIFY_SERIAL_NUMBER";

            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromActivity,
                ToActivity = toActivity,
            };

            var k2TaskData = new K2TaskData()
            {
                ActivityName = toActivity.ToString(),
                SerialNumber = currentSerialNumber
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns(Task.FromResult(k2TaskData));

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = "ASSESS_SERIAL_NUMBER"
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
            await _handler.Handle(persistWorkflowInstanceDataEvent, _handlerContext);

            //Then
            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.WorkflowInstanceId == workflowInstanceId);
            Assert.AreEqual(currentSerialNumber,
                workflowInstance.SerialNumber);
            Assert.AreEqual(toActivity.ToString(),
                workflowInstance.ActivityName);
            Assert.AreEqual(WorkflowStatus.Started.ToString(),
                workflowInstance.Status);
        }

        [TestCase(WorkflowStage.Review, WorkflowStage.Assess, WorkflowStage.Review)]
        [TestCase(WorkflowStage.Assess, WorkflowStage.Verify, WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Completed, WorkflowStage.Verify)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Assess, WorkflowStage.Verify)]
        public void Test_Handle_Given_K2Task_Is_At_Different_Stage_Than_ToActivity_Then_ApplicationException_Is_Thrown(
            WorkflowStage fromActivity, WorkflowStage toActivity, WorkflowStage k2TaskStage)
        {
            //Given
            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = 0,
                FromActivity = fromActivity,
                ToActivity = toActivity,
            };
            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns(Task.FromResult(new K2TaskData()
                {
                    ActivityName = k2TaskStage.ToString()
                }));

            //When
            var ex = Assert.ThrowsAsync<ApplicationException>(() =>
                _handler.Handle(persistWorkflowInstanceDataEvent, _handlerContext));
        }

        [TestCase(WorkflowStage.Review)]
        [TestCase(WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Verify)]
        [TestCase(WorkflowStage.Completed)]
        public void Test_Handle_Given_ToActivity_Review_Then_Exception_Is_Thrown(WorkflowStage fromActivity)
        {
            //Given
            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = 0,
                FromActivity = fromActivity,
                ToActivity = WorkflowStage.Review,
            };

            //When
            var ex = Assert.ThrowsAsync<NotImplementedException>(() =>
                _handler.Handle(persistWorkflowInstanceDataEvent, _handlerContext));

            //Then
            Assert.AreEqual($"{persistWorkflowInstanceDataEvent.ToActivity} has not been implemented for processId: {persistWorkflowInstanceDataEvent.ProcessId}.", ex.Message);
        }

        [Test]
        public async Task Test_Handle_Given_Rejected_FromActivity_Verify_And_ToActivity_Assess_Then_WorkflowInstance_Data_Is_Updated()
        {
            //Given
            var fromActivity = WorkflowStage.Verify;
            var toActivity = WorkflowStage.Assess;
            var processId = 1;
            var workflowInstanceId = 1;
            var currentSerialNumber = "VERIFY_SERIAL_NUMBER";

            var persistWorkflowInstanceDataEvent = new PersistWorkflowInstanceDataEvent()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromActivity,
                ToActivity = toActivity,
            };

            var k2TaskData = new K2TaskData()
            {
                ActivityName = toActivity.ToString(),
                SerialNumber = currentSerialNumber
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns(Task.FromResult(k2TaskData));

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = "ASSESS_SERIAL_NUMBER",
                ProductAction = new List<ProductAction> { new ProductAction
                {
                    Verified = true
                }},
                DataImpact = new List<DataImpact> { new DataImpact
                {
                    Verified = true
                }}
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var currentAssessData = new DbAssessmentAssessData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                Ion = "Assess ION"
            };
            await _dbContext.DbAssessmentAssessData.AddAsync(currentAssessData);

            var verifyData = new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                Ion = "Verify ION"
            };
            await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);
            await _dbContext.SaveChangesAsync();

            //When
            await _handler.Handle(persistWorkflowInstanceDataEvent, _handlerContext);

            //Then
            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.WorkflowInstanceId == workflowInstanceId);
            var newAssessData = await _dbContext.DbAssessmentAssessData.FirstAsync(wi => wi.WorkflowInstanceId == workflowInstanceId);

            Assert.AreEqual(newAssessData.Ion, verifyData.Ion);

            Assert.IsFalse(workflowInstance.ProductAction.First().Verified);
            Assert.IsFalse(workflowInstance.DataImpact.First().Verified);

            Assert.AreEqual(currentSerialNumber,
                workflowInstance.SerialNumber);
            Assert.AreEqual(toActivity.ToString(),
                workflowInstance.ActivityName);
            Assert.AreEqual(WorkflowStatus.Started.ToString(),
                workflowInstance.Status);
        }
    }
}
