using Common.Helpers;
using Common.Helpers.Auth;
using DbUpdatePortal.Auth;
using DbUpdatePortal.Configuration;
using DbUpdatePortal.Enums;
using DbUpdatePortal.Helpers;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace DbUpdatePortal.UnitTests
{
    [TestFixture]
    public class WorkflowTests
    {

        private DbUpdateWorkflowDbContext _dbContext;
        private WorkflowModel _workflowModel;
        private ILogger<WorkflowModel> _fakeLogger;
        private ICommentsHelper _fakeCommentsHelper;
        private ICarisProjectHelper _fakeCarisProjectHelper;
        private IPageValidationHelper _fakepageValidationHelper;
        private IOptions<GeneralConfig> _fakeGeneralConfig;
        private IAdDirectoryService _fakeAdDirectoryService;
        private IDbUpdateUserDbService _fakeNcneUserDbService;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<DbUpdateWorkflowDbContext>()
                .UseInMemoryDatabase("inmemory")
                .Options;

            _dbContext = new DbUpdateWorkflowDbContext(dbContextOptions);

            _fakeLogger = A.Dummy<ILogger<WorkflowModel>>();
            _fakeCommentsHelper = A.Fake<ICommentsHelper>();
            _fakeCarisProjectHelper = A.Fake<ICarisProjectHelper>();

            _fakepageValidationHelper = A.Fake<IPageValidationHelper>();
            _fakeGeneralConfig = A.Fake<IOptions<GeneralConfig>>();
            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakeNcneUserDbService = A.Fake<IDbUpdateUserDbService>();


            _workflowModel = new WorkflowModel(_dbContext, _fakeLogger, _fakeCommentsHelper, _fakeCarisProjectHelper,
                _fakeGeneralConfig, _fakepageValidationHelper, _fakeNcneUserDbService,
                _fakeAdDirectoryService);
        }


        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }


        [Test]
        public void Test_OnPostTaskTerminate_throws_argument_exception_when_comment_is_empty()
        {
            Assert.That((AsyncTestDelegate)(async () => await _workflowModel.OnPostTaskTerminateAsync("", 0)), Throws.ArgumentException.With.Message.Contains("is null, empty or whitespace"));
        }

        [TestCase(0, "is less than 1")]
        [TestCase(-20, "is less than 1")]
        [TestCase(9999, "does not appear in the TaskInfo table")]

        public void Test_OnPostTaskTerminate_throws_argument_exception_when_ProcessId_is_Invalid(int processId, string expectedMessage)
        {

            Assert.That((AsyncTestDelegate)(async () => await _workflowModel.OnPostTaskTerminateAsync("Valid Comment", processId)), Throws.ArgumentException.With.Message.Contains(expectedMessage));

        }

        [Test]
        public async Task Test_OnPostTaskTerminate_Terminate_the_status_for_valid_processId()
        {
            AddTaskInfo(100);

            await _workflowModel.OnPostTaskTerminateAsync("Valid Comment", 100);

            var taskInfo = _dbContext.TaskInfo.First(t => t.ProcessId == 100);

            Assert.That(taskInfo.Status, Is.EqualTo(DbUpdateTaskStatus.Terminated.ToString()));

        }
        [Test]
        public async Task Test_OnPostTaskTerminate_Adds_termination_comments_for_valid_comments()
        {
            AddTaskInfo(100);

            await _workflowModel.OnPostTaskTerminateAsync("Valid Comment", 100);

            A.CallTo(() => _fakeCommentsHelper.AddTaskComment("Terminate comment: Valid Comment", 100, A<AdUser>.Ignored))
                .MustHaveHappened();

        }


        [Test]
        public async Task Test_OnPostTaskComment_adds_TaskComments_for_valid_comments()
        {
            AddTaskInfo(100);

            await _workflowModel.OnPostTaskCommentAsync("Valid Comment", 100);

            A.CallTo(() => _fakeCommentsHelper.AddTaskComment("Valid Comment", 100, A<AdUser>.Ignored))
                .MustHaveHappened();
        }

        [Test]
        public async Task Test_OnPostTaskComment_does_not_add_TaskComments_for_Invalid_comments()
        {
            AddTaskInfo(100);

            await _workflowModel.OnPostTaskCommentAsync("", 100);

            A.CallTo(() => _fakeCommentsHelper.AddTaskComment(A<string>.Ignored, A<int>.Ignored, A<AdUser>.Ignored))
                .MustNotHaveHappened();

        }

        private void AddTaskInfo(int processId)
        {
            _dbContext.TaskInfo.Add(new TaskInfo()
            {
                ProcessId = processId,
                Status = DbUpdateTaskStatus.InProgress.ToString()
            });

            _dbContext.SaveChanges();
        }
    }
}