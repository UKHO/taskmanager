using Common.Helpers;
using Common.Helpers.Auth;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCNEPortal.Auth;
using NCNEPortal.Calculators;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using NCNEPortal.Helpers;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class WorkflowTests
    {

        private NcneWorkflowDbContext _dbContext;
        private WorkflowModel _workflowModel;
        private readonly ILogger<WorkflowModel> _fakeLogger = A.Dummy<ILogger<WorkflowModel>>();
        private readonly ICommentsHelper _fakeCommentsHelper = A.Fake<ICommentsHelper>();
        private ICarisProjectHelper _fakeCarisProjectHelper = A.Fake<ICarisProjectHelper>();
        private IMilestoneCalculator _fakeMilestoneCalculator = A.Fake<IMilestoneCalculator>();
        private readonly IPageValidationHelper _fakepageValidationHelper = A.Fake<IPageValidationHelper>();
        private readonly IOptions<GeneralConfig> _fakeGeneralConfig = A.Fake<IOptions<GeneralConfig>>();
        private readonly IAdDirectoryService _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
        private readonly INcneUserDbService _fakeNcneUserDbService = A.Fake<INcneUserDbService>();

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase("inmemory")
                .Options;

            _dbContext = new NcneWorkflowDbContext(dbContextOptions);

            _workflowModel = new WorkflowModel(_dbContext, _fakeLogger, _fakeCommentsHelper, _fakeCarisProjectHelper,
                _fakeGeneralConfig, _fakeMilestoneCalculator, _fakepageValidationHelper, _fakeNcneUserDbService,
                _fakeAdDirectoryService);
        }


        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public void Test_constructor_populates_userList_from_ncne_user_service()
        {
            A.CallTo(() => _fakeNcneUserDbService.GetUsersFromDbAsync()).Returns(new List<AdUser>
            {
                new AdUser() {DisplayName = "user 1"},
                new AdUser() {DisplayName = "user 2"}
            });

            _workflowModel = new WorkflowModel(_dbContext, _fakeLogger, _fakeCommentsHelper, _fakeCarisProjectHelper,
                _fakeGeneralConfig, _fakeMilestoneCalculator, _fakepageValidationHelper, _fakeNcneUserDbService,
                _fakeAdDirectoryService);

            Assert.That(_workflowModel.userList, Is.EqualTo(new[] { "user 1", "user 2" }));
        }

        [Test]
        public void Test_OnPostTaskTerminate_throws_argument_exception_when_comment_is_empty()
        {

            Assert.That(async () => await _workflowModel.OnPostTaskTerminateAsync("", 0), Throws.ArgumentException.With.Message.Contains("is null, empty or whitespace"));

        }

        [TestCase(0, "is less than 1")]
        [TestCase(-20, "is less than 1")]
        [TestCase(9999, "does not appear in the TaskInfo table")]

        public void Test_OnPostTaskTerminate_throws_argument_exception_when_ProcessId_is_Invalid(int processId, string expectedMessage)
        {

            Assert.That(async () => await _workflowModel.OnPostTaskTerminateAsync("Valid Comment", processId), Throws.ArgumentException.With.Message.Contains(expectedMessage));

        }

        [Test]
        public async Task Test_OnPostTaskTerminate_Terminate_the_status_for_valid_processId()
        {
            AddTaskInfo(100);

            await _workflowModel.OnPostTaskTerminateAsync("Valid Comment", 100);

            Assert.That(_dbContext.TaskInfo.Single().Status, Is.EqualTo(NcneTaskStatus.Terminated.ToString()));

        }
        [Test]
        public async Task Test_OnPostTaskTerminate_Adds_termination_comments_for_valid_comments()
        {
            AddTaskInfo(100);

            await _workflowModel.OnPostTaskTerminateAsync("Valid Comment", 100);

            A.CallTo(() => _fakeCommentsHelper.AddTaskComment("Terminate comment: Valid Comment", 100, A<string>.Ignored))
                .MustHaveHappened();

        }



        private void AddTaskInfo(int processId)
        {
            _dbContext.TaskInfo.Add(new TaskInfo()
            {
                ProcessId = processId,
                Status = NcneTaskStatus.InProgress.ToString()
            });

            _dbContext.SaveChanges();
        }
    }
}