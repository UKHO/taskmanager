using Microsoft.EntityFrameworkCore;
using NCNEPortal.Enums;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NCNEWorkflowDatabase.Tests.Helpers;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class CommentsHelperTests
    {
        private NcneWorkflowDbContext _dbContext;
        private CommentsHelper _commentsHelper;

        private AdUser testUser;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new NcneWorkflowDbContext(dbContextOptions);

            _commentsHelper = new CommentsHelper(_dbContext);

            testUser = AdUserHelper.CreateTestUser(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }


        [TestCase(NcneTaskStageType.With_SDRA)]
        [TestCase(NcneTaskStageType.With_Geodesy)]
        [TestCase(NcneTaskStageType.Specification)]
        [TestCase(NcneTaskStageType.Compile)]
        [TestCase(NcneTaskStageType.V1)]
        [TestCase(NcneTaskStageType.V1_Rework)]
        [TestCase(NcneTaskStageType.V2)]
        [TestCase(NcneTaskStageType.V2_Rework)]
        [TestCase(NcneTaskStageType.Forms)]
        [TestCase(NcneTaskStageType.Final_Updating)]
        [TestCase(NcneTaskStageType.Hundred_Percent_Check)]
        [TestCase(NcneTaskStageType.Commit_To_Print)]
        [TestCase(NcneTaskStageType.CIS)]
        [TestCase(NcneTaskStageType.Publication)]
        [TestCase(NcneTaskStageType.Publish_Chart)]
        [TestCase(NcneTaskStageType.Clear_Vector)]
        [TestCase(NcneTaskStageType.Retire_Old_Version)]
        [TestCase(NcneTaskStageType.Consider_Withdrawn_Charts)]
        [TestCase(NcneTaskStageType.Withdrawal_action)]
        [TestCase(NcneTaskStageType.PMC_withdrawal)]
        [TestCase(NcneTaskStageType.Consider_email_SDR)]
        public async Task Adding_System_Comments_for_Completion_of_stage_adds_New_Comment(NcneTaskStageType stageType)
        {
            var changeType = NcneCommentType.CompleteStage;

            //create a random processId
            var processId = 100 + (int)stageType;

            await _commentsHelper.AddTaskSystemComment(changeType, processId, testUser,
                stageType.ToString(),
                null, null);

            _dbContext.SaveChanges();

            Assert.That(_dbContext.TaskComment.Single(p => p.ProcessId == processId).Comment, Is.EqualTo(stageType.ToString() + " Step completed"));
            Assert.IsTrue(_dbContext.TaskComment.Single(p => p.ProcessId == processId).ActionIndicator);

        }

        [TestCase(NcneTaskStageType.V1)]
        [TestCase(NcneTaskStageType.V2)]
        public async Task Adding_System_Comments_for_Rework_of_stage_adds_New_Comment(NcneTaskStageType stageType)
        {
            var changeType = NcneCommentType.ReworkStage;

            //create a random processId
            var processId = 200 + (int)stageType;

            await _commentsHelper.AddTaskSystemComment(changeType, processId, testUser,
                stageType.ToString(),
                null, null);

            _dbContext.SaveChanges();

            Assert.That(_dbContext.TaskComment.Single(p => p.ProcessId == processId).Comment, Is.EqualTo(stageType.ToString() + " Step sent for Rework"));
            Assert.IsTrue(_dbContext.TaskComment.Single(p => p.ProcessId == processId).ActionIndicator);
        }

        [TestCase(NcneCommentType.CompilerChange, "Valid User1", "Compiler role changed to ")]
        [TestCase(NcneCommentType.V1Change, "Valid User2", "V1 role changed to ")]
        [TestCase(NcneCommentType.V2Change, "Valid User3", "V2 role changed to ")]
        [TestCase(NcneCommentType.HundredPcChange, "Valid User4", "100% Check role changed to ")]
        [TestCase(NcneCommentType.DateChange, "", "Task Information dates changed")]
        [TestCase(NcneCommentType.ThreePsChange, "", "3PS Details changed")]
        public async Task Adding_System_Comments_for_Date_or_Role_or_3PS_change_adds_New_Comment(NcneCommentType changeType, string roleName, string commentText)
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

        [Test]
        public async Task Adding_Task_comments_add_new_user_comment()
        {
            //create a random processId
            var processId = 400;
            var commentText = "User Comment";

            await _commentsHelper.AddTaskComment(commentText, processId, testUser);

            await _dbContext.SaveChangesAsync();

            Assert.That(_dbContext.TaskComment.Single(p => p.ProcessId == processId).Comment,
                Is.EqualTo(commentText));
            Assert.IsFalse(_dbContext.TaskComment.Single(p => p.ProcessId == processId).ActionIndicator);
        }

        [TestCase(NcneTaskStageType.Withdrawal_action)]
        public async Task Adding_TaskStage_comments_add_new_comment_for_the_stage(NcneTaskStageType taskStageType)
        {
            //create a random processId
            var processId = 500 + (int)taskStageType;
            var commentText = "User Comment";

            await _commentsHelper.AddTaskStageComment(commentText, processId, (int)taskStageType, testUser);

            await _dbContext.SaveChangesAsync();

            Assert.That(_dbContext.TaskStageComment.Single(p => p.ProcessId == processId).Comment,
                Is.EqualTo(commentText));

        }
    }
}
