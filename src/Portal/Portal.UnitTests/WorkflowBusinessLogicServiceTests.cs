using System;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Portal.BusinessLogic;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    [TestFixture]
    public class WorkflowBusinessLogicServiceTests
    {
        private WorkflowBusinessLogicService _workflowBusinessLogicService;
        private WorkflowDbContext _dbContext;
        private ILogger<WorkflowBusinessLogicService> _fakeLogger;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeLogger = A.Fake<ILogger<WorkflowBusinessLogicService>>();

            _workflowBusinessLogicService = new WorkflowBusinessLogicService(
                _dbContext,
                _fakeLogger);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [TestCase("Started", ExpectedResult = false)]
        [TestCase("Updating", ExpectedResult = false)]
        [TestCase("Unknown", ExpectedResult = false)]
        [TestCase("Terminated", ExpectedResult = true)]
        [TestCase("Completed", ExpectedResult = true)]
        public async Task<bool> Test_WorkflowIsReadOnly_Given_Provided_Status_Then_Returns_ExpectedResult(string taskStatus)
        {
            //Arrange
            var processId = 1;
            _dbContext.WorkflowInstance.Add(
            new WorkflowInstance()
            {
                ProcessId = processId,
                Status = taskStatus
            });
            await _dbContext.SaveChangesAsync();

            //Act & Assert
            return await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);
        }

        [Test]
        public void Test_WorkflowIsReadOnly_Given_ProcessId_That_Does_Not_Exist_Then_ArgumentException_Is_Thrown()
        {
            //Arrange
            var processIdThatDoesNotExist = 9999;

            //Act & Assert
            Assert.ThrowsAsync(typeof(ArgumentException),
                async () => await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processIdThatDoesNotExist)
            );
        }
    }
}
