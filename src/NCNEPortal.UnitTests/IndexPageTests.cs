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


            _dbContext.TaskInfo.Add(
                new TaskInfo()
                {
                    ChartType = "Primary",
                    WorkflowType = "NE",
                    TaskRole = new TaskRole()
                    {
                        Compiler = "Valid User1"
                    },
                    TaskStage = new List<TaskStage>()
                    {
                        new TaskStage() {TaskStageTypeId = 3, Status = "InProgress"},
                        new TaskStage() {TaskStageTypeId = 4, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 5, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 6, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 7, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 8, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 9, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 10, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 11, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 12, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 13, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 14, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 15, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 16, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 17, Status = "Open"},
                        new TaskStage() {TaskStageTypeId = 18, Status = "Open"}
                    }
                });

            _dbContext.SaveChanges();

        }


        [Test]
        public void Test_GetDeadLineStatus_method_for_FormDate_Red_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.AnnounceDate = DateTime.Now.AddDays(-1);

            var result = _indexModel.GetDeadLineStatus(task.AnnounceDate, (int)NcneTaskStageType.Forms, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Red, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_FormDate_Amber_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.AnnounceDate = DateTime.Now.AddDays(2);

            var result = _indexModel.GetDeadLineStatus(task.AnnounceDate, (int)NcneTaskStageType.Forms, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Amber, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_FormDate_Green_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.AnnounceDate = DateTime.Now.AddDays(2);

            var taskStage = task.TaskStage.Find(t => t.TaskStageTypeId == (int)NcneTaskStageType.Forms);

            taskStage.DateCompleted = DateTime.Now;
            taskStage.Status = NcneTaskStageStatus.Completed.ToString();


            var result = _indexModel.GetDeadLineStatus(task.AnnounceDate, (int)NcneTaskStageType.Forms, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Green, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_FormDate_Empty_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.AnnounceDate = DateTime.Now.AddDays(10);

            var result = _indexModel.GetDeadLineStatus(task.AnnounceDate, (int)NcneTaskStageType.Forms, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.None, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_CommitDate_Red_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.CommitDate = DateTime.Now.AddDays(-1);

            var result = _indexModel.GetDeadLineStatus(task.CommitDate, (int)NcneTaskStageType.Commit_To_Print, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Red, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_CommitDate_Amber_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.CommitDate = DateTime.Now.AddDays(2);

            var result = _indexModel.GetDeadLineStatus(task.CommitDate, (int)NcneTaskStageType.Commit_To_Print, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Amber, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_CommitDate_Green_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.CommitDate = DateTime.Now.AddDays(2);

            var taskStage = task.TaskStage.Find(t => t.TaskStageTypeId == (int)NcneTaskStageType.Commit_To_Print);

            taskStage.DateCompleted = DateTime.Now;
            taskStage.Status = NcneTaskStageStatus.Completed.ToString();


            var result = _indexModel.GetDeadLineStatus(task.CommitDate, (int)NcneTaskStageType.Commit_To_Print, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Green, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_CommitDate_Empty_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.CommitDate = DateTime.Now.AddDays(12);

            var result = _indexModel.GetDeadLineStatus(task.CommitDate, (int)NcneTaskStageType.Commit_To_Print, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.None, result);

        }
        [Test]
        public void Test_GetDeadLineStatus_method_for_CisDate_Red_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.CisDate = DateTime.Now.AddDays(-1);

            var result = _indexModel.GetDeadLineStatus(task.CisDate, (int)NcneTaskStageType.CIS, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Red, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_CisDate_Amber_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.CisDate = DateTime.Now.AddDays(2);

            var result = _indexModel.GetDeadLineStatus(task.CisDate, (int)NcneTaskStageType.CIS, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Amber, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_CisDate_Green_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.CisDate = DateTime.Now.AddDays(2);

            var taskStage = task.TaskStage.Find(t => t.TaskStageTypeId == (int)NcneTaskStageType.CIS);

            taskStage.DateCompleted = DateTime.Now;
            taskStage.Status = NcneTaskStageStatus.Completed.ToString();


            var result = _indexModel.GetDeadLineStatus(task.CisDate, (int)NcneTaskStageType.CIS, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Green, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_CisDate_Empty_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.CisDate = DateTime.Now.AddDays(12);

            var result = _indexModel.GetDeadLineStatus(task.CisDate, (int)NcneTaskStageType.CIS, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.None, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_PublishDate_Red_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.PublicationDate = DateTime.Now.AddDays(-1);

            var result = _indexModel.GetDeadLineStatus(task.PublicationDate, (int)NcneTaskStageType.Publication, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Red, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_PublishDate_Amber_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.PublicationDate = DateTime.Now.AddDays(2);

            var result = _indexModel.GetDeadLineStatus(task.PublicationDate, (int)NcneTaskStageType.Publication, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Amber, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_PublishDate_Green_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.PublicationDate = DateTime.Now.AddDays(2);

            var taskStage = task.TaskStage.Find(t => t.TaskStageTypeId == (int)NcneTaskStageType.Publication);

            taskStage.DateCompleted = DateTime.Now;
            taskStage.Status = NcneTaskStageStatus.Completed.ToString();


            var result = _indexModel.GetDeadLineStatus(task.PublicationDate, (int)NcneTaskStageType.Publication, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.Green, result);

        }

        [Test]
        public void Test_GetDeadLineStatus_method_for_PublishDate_Empty_Status()
        {
            var task = _dbContext.TaskInfo.Include(t => t.TaskStage).FirstOrDefault();

            task.PublicationDate = DateTime.Now.AddDays(12);

            var result = _indexModel.GetDeadLineStatus(task.PublicationDate, (int)NcneTaskStageType.Publication, task.TaskStage);

            Assert.AreEqual(ncneDateStatus.None, result);

        }
    }
}
