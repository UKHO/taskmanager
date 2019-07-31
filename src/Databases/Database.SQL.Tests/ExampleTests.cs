using Database.SQL.EF;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;

namespace Database.SQL.Tests
{
    public class ExampleTests
    {
        private SqliteConnection _connection;
        private DbContextOptions<TasksDbContext> _dbContextOptions;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _dbContextOptions = TasksDbBuilder.UsingConnection(_connection)
                                              .CreateTables()
                                              .PopulateTables();
        }

        [TearDown]
        public void Teardown()
        {
            _connection.Close();
        }

        [Test]
        public void Example_test()
        {
            using (var context = new TasksDbContext(_dbContextOptions))
            {
                var tasks = context.Tasks.ToList();
                Assert.AreEqual(tasks.Count, 7);
            }

        }
    }
}
