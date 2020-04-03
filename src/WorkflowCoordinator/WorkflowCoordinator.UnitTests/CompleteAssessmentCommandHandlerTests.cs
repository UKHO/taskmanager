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

    }
}
