using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkflowDatabase.EF;

namespace WorkflowDatabase.Tests
{
    public class ExampleTests
    {
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=taskmanager-dev-workflowdatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            DatabasesHelpers.ClearWorkflowDbTables(_dbContext);
        }

        [TearDown]
        public void Teardown()
        {
            _dbContext.Dispose();
        }

        // TODO discuss with test author
        //[Test]
        //public void Ensure_workflowinstance_table_prevents_duplicate_processid()
        //{
        //_dbContext.WorkflowInstance.Add(new WorkflowInstance()
        //{
        //    ProcessId = 1,
        //    SerialNumber = "1_sn",
        //    ParentProcessId = null,
        //    WorkflowType = "DbAssessment",
        //    ActivityName = "Review"
        //});

        //var ex = Assert.Throws<DbUpdateException>(() => _dbContext.WorkflowInstance.Add(new WorkflowInstance()
        //{
        //    ProcessId = 1,
        //    SerialNumber = "2_sn",
        //    ParentProcessId = null,
        //    WorkflowType = "DbAssessment",
        //    ActivityName = "Review"
        //}));

        //Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint"));
        //}

    }
}

