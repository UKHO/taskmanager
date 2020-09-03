using DbUpdatePortal.Enums;
using DbUpdatePortal.Helpers;
using DbUpdatePortal.UnitTests.Helper;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace DbUpdatePortal.UnitTests
{
    [TestFixture]
    public class CommentsHelperTests
    {
        private DbUpdateWorkflowDbContext _dbContext;
        private CommentsHelper _commentsHelper;

        private AdUser testUser;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<DbUpdateWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new DbUpdateWorkflowDbContext(dbContextOptions);

            _commentsHelper = new CommentsHelper(_dbContext);

            testUser = AdUserHelper.CreateTestUser(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }


        [TestCase(DbUpdateTaskStageType.Compile)]
        [TestCase(DbUpdateTaskStageType.Verify)]
        [TestCase(DbUpdateTaskStageType.Verification_Rework)]
        [TestCase(DbUpdateTaskStageType.SNC)]
        [TestCase(DbUpdateTaskStageType.ENC)]
        [TestCase(DbUpdateTaskStageType.Awaiting_Publication)]
        public async Task Adding_System_Comments_for_Completion_of_stage_adds_New_Comment(DbUpdateTaskStageType stageType)
        {
            var changeType = DbUpdateCommentType.CompleteStage;

            //create a random processId
            var processId = 100 + (int)stageType;

            await _commentsHelper.AddTaskSystemComment(changeType, processId, testUser,
                stageType.ToString(),
                null, null);

            _dbContext.SaveChanges();

            Assert.That(_dbContext.TaskComment.Single(p => p.ProcessId == processId).Comment, Is.EqualTo(stageType.ToString() + " Step completed"));
            Assert.IsTrue(_dbContext.TaskComment.Single(p => p.ProcessId == processId).ActionIndicator);

        }

        [TestCase(DbUpdateTaskStageType.Verify)]
        public async Task Adding_System_Comments_for_Rework_of_stage_adds_New_Comment(DbUpdateTaskStageType stageType)
        {
            var changeType = DbUpdateCommentType.ReworkStage;

            //create a random processId
            var processId = 200 + (int)stageType;

            await _commentsHelper.AddTaskSystemComment(changeType, processId, testUser,
                stageType.ToString(),
                null, null);

            _dbContext.SaveChanges();

            Assert.That(_dbContext.TaskComment.Single(p => p.ProcessId == processId).Comment, Is.EqualTo(stageType.ToString() + " Step sent for Rework"));
            Assert.IsTrue(_dbContext.TaskComment.Single(p => p.ProcessId == processId).ActionIndicator);
        }

        [TestCase(DbUpdateCommentType.CompilerChange, "Valid User1", "Compiler role changed to ")]
        [TestCase(DbUpdateCommentType.V1Change, "Valid User2", "Verifier role changed to ")]
        [TestCase(DbUpdateCommentType.DateChange, "", "Target Date changed to ")]
        public async Task Adding_System_Comments_for_Date_or_Role_change_adds_New_Comment(DbUpdateCommentType changeType, string roleName, string commentText)
        {
            //create a random processId
            var processId = 300 + (int)changeType;

            await _commentsHelper.AddTaskSystemComment(changeType, processId, testUser,
                null,
                roleName, null);

            _dbContext.SaveChanges();

            Assert.That(_dbContext.TaskComment.Single(p => p.ProcessId == processId).Comment,
                Is.EqualTo(commentText + roleName));
            Assert.IsTrue(_dbContext.TaskComment.Single(p => p.ProcessId == processId).ActionIndicator);
        }

    }
}
