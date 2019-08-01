using Database.SQL.EF;
using Database.SQL.EF.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;

namespace Database.SQL.Tests
{
    public class ExampleTests
    {
        private TasksDbContext _dbContext;
        private SqliteConnection _connection;

        [OneTimeSetUp]
        public void Setup()
        {

            // Possibility to share this and switch providers

            _connection = new SqliteConnection("DataSource=:memory:");

            var dbContextOptions = new DbContextOptionsBuilder<TasksDbContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new TasksDbContext(dbContextOptions);

            TasksDbBuilder.UsingDbContext(_dbContext)
                          .CreateTables()
                          .PopulateTables()
                          .SaveChanges();
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            // Not managed to get context dispose to take connection with it yet
            // Despite letting it open connection
            _dbContext.Database.EnsureDeleted();
            _connection.Dispose();
            _dbContext.Dispose();
        }

        [Test]
        public void Example_test()
        {
            _dbContext.Tasks.Add(new Task()
            {
                Id = 99,
                Assessor = "ben"
            });
            _dbContext.SaveChanges();

            var tasks = _dbContext.Tasks.ToList();
            Assert.AreEqual(8, tasks.Count);
        }
        [Test]
        public void Example_test_with_reset_data()
        {
            var taskAddedPreviously = _dbContext.Tasks.Find(99);
            Assert.IsNotNull(taskAddedPreviously);

            TasksDbBuilder.UsingDbContext(_dbContext)
                          .DeleteAllRowData()
                          .PopulateTables()
                          .SaveChanges();

            var tasks = _dbContext.Tasks.ToList();

            Assert.AreEqual("ben", taskAddedPreviously.Assessor);
            Assert.AreEqual(7, tasks.Count);
            Assert.IsNull(_dbContext.Tasks.Find(99));
        }
    }
}
