using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using NUnit.Framework.Internal;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

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

        [Test]
        public void Test_Comment_can_be_added_to_db_using_ef()
        {
            _dbContext.Comment.AddAsync(new WorkflowDatabase.EF.Models.Comments()
            {
               CommentId = 1,
               Created = new DateTime(),
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
