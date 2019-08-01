using Database.SQL.EF;
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

        [SetUp]
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

        [TearDown]
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
            var tasks = _dbContext.Tasks.ToList();
            Assert.AreEqual(tasks.Count, 7);
        }
    }
}
