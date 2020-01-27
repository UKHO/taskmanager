using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using NCNEWorkflowDatabase.EF;
using NUnit.Framework;


namespace NCNEWorkflowDatabase.Tests
{
    public class DatabaseIntegrityTests
    {
        private NcneWorkflowDbContext _dbContext;
        private DbContextOptions<NcneWorkflowDbContext> _dbContextOptions;

        [SetUp]
        public void Setup()
        {
            _dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseSqlServer(
                    @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=taskmanager-dev-ncneworkflowdatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
                .Options;

            _dbContext = new NcneWorkflowDbContext(_dbContextOptions);

            DatabasesHelpers.ClearNcneWorkflowDbTables(_dbContext);
        }

        [TearDown]
        public void Teardown()
        {
            _dbContext.Dispose();
        }


        [Test]
        public void Test1()
        {
            _dbContext.Database.OpenConnection();
            Assert.Pass();
        }
    }
}