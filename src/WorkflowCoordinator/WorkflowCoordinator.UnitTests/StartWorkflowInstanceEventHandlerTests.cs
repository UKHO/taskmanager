using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using Common.Messages.Enums;
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
    public class StartWorkflowInstanceEventHandlerTests
    {

        private StartChildWorkflowInstanceCommandHandler _handler;
        private IWorkflowServiceApiClient _fakeWorkflowServiceApiClient;
        private ILogger<StartChildWorkflowInstanceCommandHandler> _fakeLogger;
        private TestableMessageHandlerContext _handlerContext;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _handlerContext = new TestableMessageHandlerContext();

            _fakeWorkflowServiceApiClient = A.Fake<IWorkflowServiceApiClient>();
            _fakeLogger = A.Dummy<ILogger<StartChildWorkflowInstanceCommandHandler>>();

            _handler = new StartChildWorkflowInstanceCommandHandler(_fakeWorkflowServiceApiClient, _dbContext, _fakeLogger);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public void Test_Handle_StartWorkflowInstanceEvent_when_K2_SerialNumber_is_empty_then_Exception_Is_Thrown()
        {
            //Given
            var parentProcessId = 123;
            var childProcessId = 456;
            var dbAssessmentWorkflowId = 8;

            var startWorkflowInstanceEvent = new StartChildWorkflowInstanceCommand()
            {
                CorrelationId = Guid.Empty,
                ParentProcessId = parentProcessId,
                AssignedTaskId = 1,
                WorkflowType = WorkflowType.DbAssessment
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetDBAssessmentWorkflowId())
                .Returns(dbAssessmentWorkflowId);
            A.CallTo(() => _fakeWorkflowServiceApiClient.CreateWorkflowInstance(dbAssessmentWorkflowId))
                .Returns(childProcessId);
            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceSerialNumber(childProcessId))
                .Returns(string.Empty);

            //When
            var ex =Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(startWorkflowInstanceEvent, _handlerContext));

            //Then
            Assert.IsTrue(ex.Message.Contains($"Failed to get K2 Task serial number for ProcessId {childProcessId}"));
        }

        [Test]
        public async Task Test_Handle_StartWorkflowInstanceEvent_when_K2_returns_SerialNumber_Then_task_is_Progressed()
        {
            //Given
            var parentProcessId = 123;
            var childProcessId = 456;
            var dbAssessmentWorkflowId = 8;
            var childSerialNumber = "456_14";

            var startWorkflowInstanceEvent = new StartChildWorkflowInstanceCommand()
            {
                CorrelationId = Guid.Empty,
                ParentProcessId = parentProcessId,
                AssignedTaskId = 1,
                WorkflowType = WorkflowType.DbAssessment
            };

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetDBAssessmentWorkflowId())
                .Returns(dbAssessmentWorkflowId);
            A.CallTo(() => _fakeWorkflowServiceApiClient.CreateWorkflowInstance(dbAssessmentWorkflowId))
                .Returns(childProcessId);
            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceSerialNumber(childProcessId))
                .Returns(childSerialNumber);

            //When
            await _handler.Handle(startWorkflowInstanceEvent, _handlerContext);

            //Then
            A.CallTo(() => _fakeWorkflowServiceApiClient.ProgressWorkflowInstance(childSerialNumber))
                .MustHaveHappened();

            var persistChildWorkflowDataCommand =
                _handlerContext.SentMessages.SingleOrDefault(m => m.Message is PersistChildWorkflowDataCommand);
            Assert.IsNotNull(persistChildWorkflowDataCommand);
        }


        [Test]
        public async Task Test_Handle_PersistChildWorkflowDataCommand_when_new_WorkflowInstance_for_child_is_created_Then_parent_data_is_copied()
        {
            //Given
            var parentProcessId = 123;
            var childProcessId = 456;
            var childSerialNumber = "456_14";
            var assignedTaskId = 1;
            var primarySdocId = 1888403;

            var parentWorkflowinstanceData = new WorkflowInstance()
            {
                ProcessId = parentProcessId,
                PrimarySdocId = primarySdocId,
                SerialNumber = "445_14",
                ParentProcessId = null,
                ActivityName = WorkflowStage.Review.ToString(),
                StartedAt = DateTime.Now,
                Status = WorkflowStatus.Started.ToString(),
                ActivityChangedAt = DateTime.Today
            };

            await _dbContext.WorkflowInstance.AddAsync(parentWorkflowinstanceData);
            await _dbContext.SaveChangesAsync();

            var parentReviewData = new DbAssessmentReviewData()
            {
                ProcessId = parentProcessId,
                WorkflowInstanceId = parentWorkflowinstanceData.WorkflowInstanceId,
                ActivityCode = "Some code",
                Ion = "Parent Review ION",
                Reviewer = "reviewer1"
            };

            await _dbContext.DbAssessmentReviewData.AddAsync(parentReviewData);

            var parentAssignedTaskData = new DbAssessmentAssignTask()
            {
                DbAssessmentAssignTaskId = assignedTaskId,
                ProcessId = parentProcessId,
                Assessor = "assessor1",
                TaskType = "Type 1",
                Notes = "A note",
                Verifier = "verifier1",
                WorkspaceAffected = "Workspace 1"

            };

            await _dbContext.DbAssessmentAssignTask.AddAsync(parentAssignedTaskData);

            var parentAssessmentData = new AssessmentData()
            {
                ProcessId = parentProcessId,
                PrimarySdocId = primarySdocId,
                RsdraNumber = "RSDRA2017000130865"
            };

            await _dbContext.AssessmentData.AddAsync(parentAssessmentData);

            var parentPrimarydocument = new PrimaryDocumentStatus()
            {
                //PrimaryDocumentStatusId = 1,
                ContentServiceId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                ProcessId = parentProcessId,
                SdocId = primarySdocId,
                StartedAt = DateTime.Today,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            };
            await _dbContext.PrimaryDocumentStatus.AddAsync(parentPrimarydocument);

            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceSerialNumber(childProcessId))
                .Returns(childSerialNumber);

            var persistChildWorkflowDataCommand = new PersistChildWorkflowDataCommand()
            {
                AssignedTaskId = assignedTaskId,
                CorrelationId = Guid.NewGuid(),
                ParentProcessId = parentProcessId,
                ChildProcessId = childProcessId
            };

            //When
            await _handler.Handle(persistChildWorkflowDataCommand, _handlerContext);

            //Then

            // Assert AssessmentData
            var childAssessmentData =
                await _dbContext.AssessmentData.SingleOrDefaultAsync(ad => ad.ProcessId == childProcessId);

            Assert.IsNotNull(childAssessmentData);
            Assert.AreEqual(parentAssessmentData.PrimarySdocId, childAssessmentData.PrimarySdocId);
            Assert.AreEqual(parentAssessmentData.RsdraNumber, childAssessmentData.RsdraNumber);

            // Assert PrimaryDocumentStatus
            var childPrimaryDocumentStatus =
                await _dbContext.PrimaryDocumentStatus.SingleOrDefaultAsync(ad => ad.ProcessId == childProcessId);

            Assert.IsNotNull(childPrimaryDocumentStatus);
            Assert.AreEqual(parentPrimarydocument.ContentServiceId, childPrimaryDocumentStatus.ContentServiceId);
            Assert.AreEqual(parentPrimarydocument.SdocId, childPrimaryDocumentStatus.SdocId);

            // Assert Parent Review and AssignedTasks persisted into child Assess data
            var childDbAssessmentAssessData =
                await _dbContext.DbAssessmentAssessData.SingleOrDefaultAsync(ad => ad.ProcessId == childProcessId);

            Assert.IsNotNull(childDbAssessmentAssessData);
            Assert.AreEqual(parentReviewData.ActivityCode, childDbAssessmentAssessData.ActivityCode);
            Assert.AreEqual(parentReviewData.Ion, childDbAssessmentAssessData.Ion);
            Assert.AreEqual(parentReviewData.Reviewer, childDbAssessmentAssessData.Reviewer);
            Assert.AreEqual(parentAssignedTaskData.Assessor, childDbAssessmentAssessData.Assessor);
            Assert.AreEqual(parentAssignedTaskData.Verifier, childDbAssessmentAssessData.Verifier);
            Assert.AreEqual(parentAssignedTaskData.TaskType, childDbAssessmentAssessData.TaskType);
            Assert.AreEqual(parentAssignedTaskData.WorkspaceAffected, childDbAssessmentAssessData.WorkspaceAffected);

            // Assert Parent Assigned Task Notes persisted into child comments
            var childComments =
                await _dbContext.Comment.Where(ad => ad.ProcessId == childProcessId).Select(c => c.Text).ToListAsync();

            var newChildComment =
                $"Assign Task (Parent processId: {parentProcessId}): {parentAssignedTaskData.Notes.Trim()}";

            Assert.IsNotNull(childComments);
            Assert.Greater(childComments.Count, 0);
            Assert.Contains(newChildComment, childComments);
        }

        [Test]
        public async Task Test_Handle_PersistChildWorkflowDataCommand_when_child_workflow_is_progressed_to_Assess_Then_WorkflowInstance_ActivityChangedAt_Is_Updated()
        {
            //Given
            var parentProcessId = 123;
            var childProcessId = 456;
            var childSerialNumber = "456_14";
            var assignedTaskId = 1;
            var primarySdocId = 1888403;
            var newActivityChangedAt = DateTime.Today;

            var parentWorkflowinstanceData = new WorkflowInstance()
            {
                ProcessId = parentProcessId,
                PrimarySdocId = primarySdocId,
                SerialNumber =  "445_14",
                ParentProcessId =  null,
                ActivityName =  WorkflowStage.Review.ToString(),
                StartedAt =  DateTime.Now,
                Status =  WorkflowStatus.Started.ToString(),
                ActivityChangedAt = newActivityChangedAt
            };

            await _dbContext.WorkflowInstance.AddAsync(parentWorkflowinstanceData);
            await _dbContext.SaveChangesAsync();

            var parentReviewData = new DbAssessmentReviewData()
            {
                ProcessId = parentProcessId,
                WorkflowInstanceId = parentWorkflowinstanceData.WorkflowInstanceId,
                ActivityCode = "Some code",
                Ion = "Parent Review ION",
                Reviewer = "reviewer1"
            };

            await _dbContext.DbAssessmentReviewData.AddAsync(parentReviewData);

            var parentAssignedTaskData = new DbAssessmentAssignTask()
            {
                DbAssessmentAssignTaskId = assignedTaskId,
                ProcessId = parentProcessId,
                Assessor = "assessor1",
                TaskType = "Type 1",
                Notes = "A note",
                Verifier = "verifier1",
                WorkspaceAffected = "Workspace 1"

            };

            await _dbContext.DbAssessmentAssignTask.AddAsync(parentAssignedTaskData);

            var parentAssessmentData = new AssessmentData()
            {
                ProcessId = parentProcessId,
                PrimarySdocId = primarySdocId,
                RsdraNumber = "RSDRA2017000130865"
            };

            await _dbContext.AssessmentData.AddAsync(parentAssessmentData);

            var parentPrimarydocument = new PrimaryDocumentStatus()
            {
                //PrimaryDocumentStatusId = 1,
                ContentServiceId = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(),
                ProcessId = parentProcessId,
                SdocId = primarySdocId,
                StartedAt = DateTime.Today,
                Status = SourceDocumentRetrievalStatus.FileGenerated.ToString()
            };
            await _dbContext.PrimaryDocumentStatus.AddAsync(parentPrimarydocument);

            await _dbContext.SaveChangesAsync();

            A.CallTo(() => _fakeWorkflowServiceApiClient.GetWorkflowInstanceSerialNumber(childProcessId))
                .Returns(childSerialNumber);

            var persistChildWorkflowDataCommand = new PersistChildWorkflowDataCommand()
            {
                AssignedTaskId = assignedTaskId,
                CorrelationId = Guid.NewGuid(),
                ParentProcessId = parentProcessId,
                ChildProcessId = childProcessId
            };

            //When
            await _handler.Handle(persistChildWorkflowDataCommand, _handlerContext);

            //Then
            var childWorkflowInstance = await _dbContext.WorkflowInstance.FirstAsync(wi => wi.ProcessId == childProcessId);

            Assert.AreEqual(newActivityChangedAt, childWorkflowInstance.ActivityChangedAt);
        }

    }
}
