using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WorkflowDatabase.EF;

namespace Portal.UnitTests
{
    [TestFixture]
    public class SqlEfTests
    {
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public void Test_Comment_can_be_added_to_db_using_ef()
        {
            _dbContext.Comment.AddAsync(new WorkflowDatabase.EF.Models.Comment()
            {
                CommentId = 1,
                Created = DateTime.Now,
                ProcessId = 9876,
                Text = "This is a comment",
                Username = "Me",
                WorkflowInstanceId = 555
            });

            _dbContext.SaveChanges();

            Assert.AreEqual(_dbContext.Comment.FirstAsync(c => c.CommentId == 1).Result.Text, "This is a comment");
        }

    }
}
