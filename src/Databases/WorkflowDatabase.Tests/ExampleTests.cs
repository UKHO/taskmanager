using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;
using Common.Helpers;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

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

            TasksDbBuilder.UsingDbContext(_dbContext)
                          .PopulateTables()
                          .SaveChanges();
        }

        [TearDown]
        public void Teardown()
        {
            _dbContext.Dispose();
        }

        [Test]
        public void Example_test()
        {
            _dbContext.Tasks.Add(new Task()
            {
                TaskId = 99,
                WorkflowProcessId = 9999,
                Assessor = "ben"

            });
            _dbContext.SaveChanges();

            var tasks = _dbContext.Tasks.ToList();
            Assert.AreEqual(8, tasks.Count);
        }

        [Test]
        public void Ensure_workflowinstance_table_prevents_duplicate_processid()
        {
            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "1_sn",
                ParentProcessId = null,
                WorkflowType = "DbAssessment",
                ActivityName = "Review"
            });

            _dbContext.WorkflowInstance.Add(new WorkflowInstance()
            {
                ProcessId = 1,
                SerialNumber = "2_sn",
                ParentProcessId = null,
                WorkflowType = "DbAssessment",
                ActivityName = "Review"
            });

            var ex = Assert.Throws<DbUpdateException>(() => _dbContext.SaveChanges());
            Assert.That(ex.InnerException.Message.Contains("Violation of UNIQUE KEY constraint"));
        }

    }

}

