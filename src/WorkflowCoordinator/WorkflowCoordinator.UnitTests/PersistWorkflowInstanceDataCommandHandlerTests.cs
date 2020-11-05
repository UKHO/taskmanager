using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.Events;
using WorkflowCoordinator.Handlers;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Models;
using WorkflowCoordinator.UnitTests.Helpers;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.UnitTests
{
    public class PersistWorkflowInstanceDataCommandHandlerTests
    {
        private TestableMessageHandlerContext _handlerContext;
        private PersistWorkflowInstanceDataCommandHandler _handler;
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private ILogger<PersistWorkflowInstanceDataCommandHandler> _fakeLogger;
        private IPcpEventServiceApiClient _fakePcpEventServiceApiClient;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakePcpEventServiceApiClient = A.Fake<IPcpEventServiceApiClient>();
            _fakeLogger = A.Dummy<ILogger<PersistWorkflowInstanceDataCommandHandler>>();

            _handlerContext = new TestableMessageHandlerContext();

            _handler = new PersistWorkflowInstanceDataCommandHandler(_fakeWorkflowServiceApiClient,
                _fakeLogger, _dbContext,
                _fakePcpEventServiceApiClient);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [TestCase(WorkflowStage.Review, WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Assess, WorkflowStage.Verify)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Rejected)]
        public async Task Test_Handle_Given_task_successfully_progressed_in_K2_Then_WorkflowInstance_Data_Is_Updated(
                                                                                                                    WorkflowStage fromAction,
                                                                                                                    WorkflowStage toAction)
        {
            //Given
            var processId = 1;
            var workflowInstanceId = 1;
            var currentSerialNumber = $"{fromAction}_SERIAL_NUMBER";
            var newSerialNumber = $"{toAction}_SERIAL_NUMBER";

            var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromAction,
                ToActivity = toAction,
            };

            var storedToAction = toAction == WorkflowStage.Rejected
                ? WorkflowStage.Assess.ToString()
                : toAction.ToString();

            var k2TaskData = new K2TaskData()
            {
                ActivityName = storedToAction,
                SerialNumber = newSerialNumber
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns(Task.FromResult(k2TaskData));

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromAction.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = currentSerialNumber
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var reviewData = new DbAssessmentReviewData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                Reviewer = AdUserHelper.CreateTestUser(_dbContext)
            };
            await _dbContext.DbAssessmentReviewData.AddAsync(reviewData);

            var assessData = new DbAssessmentAssessData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentAssessData.AddAsync(assessData);

            var verifyData = new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);
            await _dbContext.SaveChangesAsync();

            //When
            await _handler.Handle(persistWorkflowInstanceDataCommand, _handlerContext);

            //Then
            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.WorkflowInstanceId == workflowInstanceId);
            Assert.AreEqual(newSerialNumber,
                workflowInstance.SerialNumber);
            Assert.AreEqual(storedToAction,
                workflowInstance.ActivityName);
            Assert.AreEqual(WorkflowStatus.Started.ToString(),
                workflowInstance.Status);
        }

        [TestCase(WorkflowStage.Review, WorkflowStage.Terminated)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Completed)]
        public async Task Test_Handle_Given_successfully_Terminated_or_SignedOff_Task_Then_CompleteAssessmentCommand_Is_Fired(
                                                                                                                        WorkflowStage fromActivity,
                                                                                                                        WorkflowStage toActivity)
        {
            //Given
            var processId = 1;
            var workflowInstanceId = 1;

            var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromActivity,
                ToActivity = toActivity,
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns((K2TaskData)null);

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = $"{fromActivity}_SERIAL_NUMBER"
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var reviewData = new DbAssessmentReviewData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentReviewData.AddAsync(reviewData);

            var verifyData = new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);

            var primaryDocumentStatus = new PrimaryDocumentStatus()
            {
                ProcessId = processId,
                SdocId = 1234567
            };
            await _dbContext.PrimaryDocumentStatus.AddAsync(primaryDocumentStatus);
            await _dbContext.SaveChangesAsync();

            //When
            await _handler.Handle(persistWorkflowInstanceDataCommand, _handlerContext);

            //Then
            Assert.AreEqual(1, _handlerContext.SentMessages.Length);

            var completeAssessmentCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is CompleteAssessmentCommand);
            Assert.IsNotNull(completeAssessmentCommand, $"No message of type {nameof(CompleteAssessmentCommand)} seen.");
        }

        [TestCase(WorkflowStage.Review, WorkflowStage.Assess, WorkflowStage.Review)]
        [TestCase(WorkflowStage.Assess, WorkflowStage.Verify, WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Completed, WorkflowStage.Verify)]
        [TestCase(WorkflowStage.Verify, WorkflowStage.Rejected, WorkflowStage.Verify)]
        public void Test_Handle_Given_K2Task_Is_At_Different_Stage_Than_ToActivity_Then_ApplicationException_Is_Thrown(
            WorkflowStage fromActivity, WorkflowStage toActivity, WorkflowStage k2TaskStage)
        {
            //Given
            var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
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
                _handler.Handle(persistWorkflowInstanceDataCommand, _handlerContext));
        }

        [TestCase(WorkflowStage.Review)]
        [TestCase(WorkflowStage.Assess)]
        [TestCase(WorkflowStage.Verify)]
        [TestCase(WorkflowStage.Completed)]
        public void Test_Handle_Given_ToActivity_Review_Then_Exception_Is_Thrown(WorkflowStage fromActivity)
        {
            //Given
            var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
            {
                CorrelationId = Guid.Empty,
                ProcessId = 0,
                FromActivity = fromActivity,
                ToActivity = WorkflowStage.Review,
            };

            //When
            var ex = Assert.ThrowsAsync<NotImplementedException>(() =>
                _handler.Handle(persistWorkflowInstanceDataCommand, _handlerContext));

            //Then
            Assert.AreEqual($"{persistWorkflowInstanceDataCommand.ToActivity} has not been implemented for processId: {persistWorkflowInstanceDataCommand.ProcessId}.", ex.Message);
        }


        [Test]
        [TestCase("Review", "Assess", "Assess")]
        [TestCase("Assess", "Verify", "Verify")]
        [TestCase("Verify", "Completed", "")]
        [TestCase("Verify", "Assess", "Assess")]
        public async Task Test_Handle_when_status_changes_Then_WorkflowInstance_ActivityChangedAt_Is_Updated(string fromAction, string toAction, string k2ActivityName)
        {
            //Given
            var fromActivity = Enum.Parse<WorkflowStage>(fromAction);
            var toActivity = Enum.Parse<WorkflowStage>(toAction);
            var processId = 1;
            var workflowInstanceId = 1;
            var currentSerialNumber = "k2-serialNumber";
            var currentActivityChangedAt = DateTime.Today.AddDays(-1);
            var newActivityChangedAt = DateTime.Today;

            var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = processId,
                FromActivity = fromActivity,
                ToActivity = toActivity,
            };

            K2TaskData k2TaskData = string.IsNullOrWhiteSpace(k2ActivityName) ? null : new K2TaskData()
            {
                ActivityName = k2ActivityName,
                SerialNumber = currentSerialNumber
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns(k2TaskData);

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                ActivityChangedAt = currentActivityChangedAt,
                SerialNumber = "ASSESS_SERIAL_NUMBER",
                ProductAction = new List<ProductAction> { new ProductAction
                {
                    Verified = true
                }},
                SncAction = new List<SncAction> { new SncAction
                {
                    Verified = true
                }},
                DataImpact = new List<DataImpact> { new DataImpact
                {
                    FeaturesVerified = true
                }}
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            await _dbContext.DbAssessmentReviewData.AddAsync(new DbAssessmentReviewData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                Ion = "Review ION"
            });

            await _dbContext.DbAssessmentAssessData.AddAsync(new DbAssessmentAssessData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                Ion = "Assess ION"
            });

            await _dbContext.DbAssessmentVerifyData.AddAsync(new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId,
                Ion = "Verify ION"
            });


            var primaryDocumentStatus = new PrimaryDocumentStatus()
            {
                ProcessId = processId,
                SdocId = 1234567
            };
            await _dbContext.PrimaryDocumentStatus.AddAsync(primaryDocumentStatus);
            await _dbContext.SaveChangesAsync();

            //When
            await _handler.Handle(persistWorkflowInstanceDataCommand, _handlerContext);

            //Then
            var workflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.WorkflowInstanceId == workflowInstanceId);

            Assert.AreEqual(newActivityChangedAt, workflowInstance.ActivityChangedAt);
        }

        [Test]
        public async Task Test_Handle_SignOff_FromActivity_Verify_And_ToActivity_Completed__then_SourceDocumentVerifiedEvent_is_published()
        {
            var processId = 1;
            var workflowInstanceId = 1;
            var fromActivity = WorkflowStage.Verify;
            var toActivity = WorkflowStage.Completed;
            var sdocId = 123456;

            var persistWorkflowInstanceDataCommand = new PersistWorkflowInstanceDataCommand()
            {
                CorrelationId = Guid.Empty,
                ProcessId = processId,
                FromActivity = fromActivity,
                ToActivity = toActivity,
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceData(A<int>.Ignored))
                .Returns((K2TaskData)null);

            var currentWorkflowInstance = new WorkflowInstance()
            {
                ProcessId = processId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityName = fromActivity.ToString(),
                Status = WorkflowStatus.Updating.ToString(),
                SerialNumber = $"{fromActivity}_SERIAL_NUMBER"
            };
            await _dbContext.WorkflowInstance.AddAsync(currentWorkflowInstance);
            await _dbContext.SaveChangesAsync();

            var reviewData = new DbAssessmentReviewData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentReviewData.AddAsync(reviewData);

            var verifyData = new DbAssessmentVerifyData()
            {
                WorkflowInstanceId = workflowInstanceId,
                ProcessId = processId
            };
            await _dbContext.DbAssessmentVerifyData.AddAsync(verifyData);

            var primaryDocumentStatus = new PrimaryDocumentStatus()
            {
                ProcessId = processId,
                SdocId = sdocId
            };
            await _dbContext.PrimaryDocumentStatus.AddAsync(primaryDocumentStatus);
            await _dbContext.SaveChangesAsync();

            //When
            await _handler.Handle(persistWorkflowInstanceDataCommand, _handlerContext);

            //Then

            A.CallTo(() =>
                    _fakePcpEventServiceApiClient.PostEvent(
                                                        nameof(SourceDocumentVerifiedEvent),
                                                        A<SourceDocumentVerifiedEvent>.That.Matches(c => c.SourceDocumentId == sdocId && c.TaskManagerProcessId == processId)))
                .MustHaveHappened();


        }

    }
}
