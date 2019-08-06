using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowDatabase.Tests
{
    public class ExampleTests
    {
        private WorkflowDbContext _dbContext;
        private SqliteConnection _connection;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");

            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            TasksDbBuilder.UsingDbContext(_dbContext)
                          .PopulateTables()
                          .SaveChanges();
        }

        [TearDown]
        public void Teardown()
        {
            _connection.Dispose();
            _dbContext.Dispose();
        }

        [Test]
        public void Example_test()
        {
            _dbContext.Tasks.Add(new Task()
            {
                //   Id = 99,
                Assessor = "ben"
            });
            _dbContext.SaveChanges();

            var tasks = _dbContext.Tasks.ToList();
            Assert.AreEqual(8, tasks.Count);
        }
    }
}

