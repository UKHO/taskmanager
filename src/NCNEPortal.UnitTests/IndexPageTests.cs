using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCNEPortal.Auth;
using NCNEPortal.Enums;
using NCNEPortal.Pages;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NCNEPortal.UnitTests
{
    [TestFixture]
    public class IndexPageTests
    {
        private IndexModel _indexModel;
        private IUserIdentityService _fakeUserIdentityService;
        private NcneWorkflowDbContext _dbContext;
        private ILogger<IndexModel> _fakeLogger;
        private IDirectoryService _fakeDirectoryService;
        private List<TaskStage> _taskStages;


        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;


            _dbContext = new NcneWorkflowDbContext(dbContextOptions);

            _fakeUserIdentityService = A.Fake<IUserIdentityService>();
            _fakeDirectoryService = A.Fake<IDirectoryService>();

            _fakeLogger = A.Fake<ILogger<IndexModel>>();

            _indexModel = new IndexModel(_fakeUserIdentityService, _dbContext, _fakeLogger, _fakeDirectoryService);




        }

        [TestCase(-3, ncneDateStatus.Red)]
        [TestCase(0, ncneDateStatus.Red)]
        [TestCase(1, ncneDateStatus.Amber)]
        [TestCase(7, ncneDateStatus.Amber)]
        [TestCase(8, ncneDateStatus.None)]
        [TestCase(100, ncneDateStatus.None)]

        public void GetDeadLineStatus_for_GivenDate_and_Open_stage_gives_Expected_Status(int daysUntilDeadline, ncneDateStatus expected)
        {
            var inputdate = DateTime.Now.AddDays(daysUntilDeadline);

            _taskStages = new List<TaskStage>()
            {
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Specification, Status = "InProgress"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Compile, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1_Rework, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2_Rework, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Forms, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Final_Updating, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Hundred_Percent_Check, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Commit_To_Print, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.CIS, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publication, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publish_Chart, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Clear_Vector, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Retire_Old_Version, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Consider_Withdrawn_Charts, Status = "Open"}
            };

            var result = _indexModel.GetDeadLineStatus(inputdate, NcneTaskStageType.Forms, _taskStages);

            Assert.AreEqual(expected, result);

        }

        [TestCase(-3, ncneDateStatus.Red)]
        [TestCase(0, ncneDateStatus.Red)]
        [TestCase(1, ncneDateStatus.Amber)]
        [TestCase(7, ncneDateStatus.Amber)]
        [TestCase(8, ncneDateStatus.None)]
        [TestCase(100, ncneDateStatus.None)]

        public void GetDeadLineStatus_for_GivenDate_and_InProgress_stage_gives_Expected_Status(int daysUntilDeadline, ncneDateStatus expected)
        {
            var inputdate = DateTime.Now.AddDays(daysUntilDeadline);

            _taskStages = new List<TaskStage>()
            {
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Specification, Status = "InProgress"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Compile, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1_Rework, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2_Rework, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Forms, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Final_Updating, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Hundred_Percent_Check, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Commit_To_Print, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.CIS, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publication, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publish_Chart, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Clear_Vector, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Retire_Old_Version, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Consider_Withdrawn_Charts, Status = "Open"}
            };

            var result = _indexModel.GetDeadLineStatus(inputdate, NcneTaskStageType.Specification, _taskStages);

            Assert.AreEqual(expected, result);

        }



        [TestCase(-3)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(100)]

        public void GetDeadLineStatus_for_Any_Date_and_Completed_stage_gives_Green_Status(int daysUntilDeadline)
        {
            var inputdate = DateTime.Now.AddDays(daysUntilDeadline);

            _taskStages = new List<TaskStage>()
            {
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Specification, Status = "Completed"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Compile, Status = "Completed"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1, Status = "Completed"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1_Rework, Status = "Completed"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2, Status = "Completed"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2_Rework, Status = "Completed"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Forms, Status = "Completed"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Final_Updating, Status = "InProgress"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Hundred_Percent_Check, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Commit_To_Print, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.CIS, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publication, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publish_Chart, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Clear_Vector, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Retire_Old_Version, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Consider_Withdrawn_Charts, Status = "Open"}
            };

            var result = _indexModel.GetDeadLineStatus(inputdate, NcneTaskStageType.Forms, _taskStages);

            Assert.AreEqual(ncneDateStatus.Green, result);

        }

        [TestCase("InProgress")]
        [TestCase("Open")]
        public void GetDeadLineStatus_for_stage_with_Null_Date_gives_None_Status(string taskStatus)
        {
            DateTime? inputdate = null;

            _taskStages = new List<TaskStage>()
            {
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Specification, Status = taskStatus},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Compile, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V1_Rework, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.V2_Rework, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Forms, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Final_Updating, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Hundred_Percent_Check, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Commit_To_Print, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.CIS, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publication, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publish_Chart, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Clear_Vector, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Retire_Old_Version, Status = "Open"},
                new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Consider_Withdrawn_Charts, Status = "Open"}
            };

            var result = _indexModel.GetDeadLineStatus(inputdate, NcneTaskStageType.Specification, _taskStages);

            Assert.AreEqual(ncneDateStatus.None, result);
        }

        [Test]
        public async Task OnGetAsync_sets_UserFullName_from_userIdentityService()
        {
            A.CallTo(() => _fakeUserIdentityService.GetFullNameForUser(A<ClaimsPrincipal>.Ignored))
                .Returns("The User's Full Name");

            await _indexModel.OnGetAsync();

            Assert.AreEqual("The User's Full Name", _indexModel.UserFullName);
        }


        [Test]
        public async Task OnGetAsync_updates_NcneTasks_with_Deadline_Status()
        {
            var task = _dbContext.TaskInfo.Add(new TaskInfo()
            {
                AnnounceDate = DateTime.Now.AddDays(-3),
                CommitDate = DateTime.Now.AddDays(1),
                CisDate = DateTime.Now.AddDays(10),
                PublicationDate = DateTime.Now.AddDays(15),
                TaskStage = new List<TaskStage>()
                {
                    new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Forms, Status = "Open"},
                    new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Commit_To_Print, Status = "Open"},
                    new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.CIS, Status = "InProgress"},
                    new TaskStage {TaskStageTypeId = (int) NcneTaskStageType.Publication, Status = "Completed"},

                }
            });

            _dbContext.SaveChanges();

            await _indexModel.OnGetAsync();

            Assert.AreEqual((int)ncneDateStatus.Red, _indexModel.NcneTasks.Single().FormDateStatus);
            Assert.AreEqual((int)ncneDateStatus.Amber, _indexModel.NcneTasks.Single().CommitDateStatus);
            Assert.AreEqual((int)ncneDateStatus.None, _indexModel.NcneTasks.Single().CisDateStatus);
            Assert.AreEqual((int)ncneDateStatus.Green, _indexModel.NcneTasks.Single().PublishDateStatus);
        }

    }
}
