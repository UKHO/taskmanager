using System;
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
using WorkflowCoordinator.Sagas;
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

            //var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            //    .UseInMemoryDatabase(databaseName: "inmemory")
            //    .Options;

            //_dbContext = new WorkflowDbContext(dbContextOptions);

            //var generalConfigOptionsSnapshot = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            //var generalConfig = new GeneralConfig { WorkflowCoordinatorAssessmentPollingIntervalSeconds = 5, CallerCode = "HDB" };
            //A.CallTo(() => generalConfigOptionsSnapshot.Value).Returns(generalConfig);

            //_fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            //_fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            //_fakeMapper = A.Fake<IMapper>();
            //_fakeLogger = A.Dummy<ILogger<StartDbAssessmentSaga>>();
            //A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceSerialNumber(A<int>.Ignored)).Returns("1234");


            //_saga = new StartDbAssessmentSaga(generalConfigOptionsSnapshot,
            //    _fakeDataServiceApiClient,
            //    _fakeWorkflowServiceApiClient,
            //    _dbContext,
            //    _fakeMapper,
            //    _fakeLogger)
            //{ Data = new StartDbAssessmentSagaData() };
            //_handlerContext = new TestableMessageHandlerContext();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }


        //1.Workflow Data is set:
        //Review -> Assess
        //Assess -> Verify
        //Verify -> Assess
        //Verify -> Completed

        //2.specific data set

        //? -> Review DONE

        //K2 task is at incorrect stage DONE

        //Junk message data?

        [Test]
        public async Task Test_Handle_Given_FromActivity_Review_And_ToActivity_Assess_Then_WorkflowInstance_Data_Is_Updated()
        {
            //Given
            var fromActivity = WorkflowStage.Review;
            var toActivity = WorkflowStage.Assess;
            var processId = 1;
            var workflowInstanceId = 1;
            var serialNumber = "TEST_SERIAL_NUMBER";

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
                SerialNumber = serialNumber
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns(Task.FromResult(k2TaskData));

            var assessmentData = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId
            };
            await _dbContext.WorkflowInstance.AddAsync(assessmentData);
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
            Assert.AreEqual(serialNumber, 
                workflowInstance.SerialNumber);
            Assert.AreEqual(toActivity.ToString(),
                workflowInstance.ActivityName);
            Assert.AreEqual(WorkflowStatus.Started.ToString(),
                workflowInstance.Status);

            var assessData = await _dbContext.DbAssessmentAssessData.FirstOrDefaultAsync(assess => assess.WorkflowInstanceId == workflowInstanceId);
            Assert.IsNotNull(assessData);
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

            //Then
            //Assert.AreEqual($"{persistWorkflowInstanceDataEvent.ToActivity} has not been implemented for processId: {persistWorkflowInstanceDataEvent.ProcessId}.", ex.Message);
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
        public void Test_Handle_Given_FromActivity_Assess_And_ToActivity_Verify_Then()
        {
            //Given

            //When

            //Then
            Assert.AreEqual("asda", "asd");
        }
    }
}
