using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Portal.Auth;
using Portal.Pages;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class TaskNoteTests
    {
        private WorkflowDbContext _dbContext;
        private int ProcessId { get; set; }
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private IMapper _fakeMapper;
        private IUserIdentityService _fakeUserIdentityService;

        [SetUp]
        public async Task Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            _fakeMapper = A.Fake<IMapper>();
            _fakeUserIdentityService = A.Fake<IUserIdentityService>();

            ProcessId = 123;

            _dbContext.WorkflowInstance.Add(new WorkflowInstance
            {
                ProcessId = ProcessId,
                ActivityName = "Review",
                AssessmentData = new AssessmentData(),
                Comments = new List<Comment>(),
                OnHold = new List<OnHold>(),
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                SerialNumber = "123_sn",
                StartedAt = DateTime.Now.AddDays(-3),
                Status = WorkflowStatus.Started.ToString(),
                WorkflowType = "DbAssessment"
            });

            await _dbContext.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task New_Task_Note_Is_Added_When_None_Exists()
        {
            var taskNote = new TaskNote
            {
                ProcessId = ProcessId,
                WorkflowInstanceId = 1,
                Text = "Test task note",
                Created = DateTime.Now,
                CreatedByUsername = "Tests",
                LastModified = DateTime.Now,
                LastModifiedByUsername = "Tests"
            };
            var indexModel = new IndexModel(_dbContext,
                _fakeMapper,
                _fakeUserIdentityService);

            await indexModel.OnPostTaskNoteAsync(taskNote.Text, ProcessId);

            Assert.IsNotEmpty(_dbContext.TaskNote.FirstAsync(tn => tn.ProcessId == ProcessId).Result.Text);
        }

        [Test]
        public async Task New_Task_Note_Is_Not_Added_When_Task_Note_Text_Is_Empty()
        {
            var taskNote = new TaskNote
            {
                ProcessId = ProcessId,
                WorkflowInstanceId = 1,
                Text = string.Empty,
                Created = DateTime.Now,
                CreatedByUsername = "Tests",
                LastModified = DateTime.Now,
                LastModifiedByUsername = "Tests"
            };

            var indexModel = new IndexModel(_dbContext,
                _fakeMapper,
                _fakeUserIdentityService);

            await indexModel.OnPostTaskNoteAsync(taskNote.Text, ProcessId);

            Assert.AreEqual(null, _dbContext.TaskNote.FirstOrDefault(tn => tn.ProcessId == ProcessId));
        }

        [Test]
        public async Task Task_Note_Is_Updated_When_One_Already_Exists()
        {
            var taskNote = new TaskNote
            {
                ProcessId = ProcessId,
                WorkflowInstanceId = 1,
                Text = "Test task note",
                Created = DateTime.Now,
                CreatedByUsername = "Tests",
                LastModified = DateTime.Now,
                LastModifiedByUsername = "Tests"
            };

            var updatedTaskNote = new TaskNote
            {
                ProcessId = ProcessId,
                WorkflowInstanceId = 1,
                Text = "Test task note for update",
                LastModified = DateTime.Now,
                LastModifiedByUsername = "Tests"
            };

            await _dbContext.TaskNote.AddAsync(taskNote);
            await _dbContext.SaveChangesAsync();

            var indexModel = new IndexModel(_dbContext,
                _fakeMapper,
                _fakeUserIdentityService);

            await indexModel.OnPostTaskNoteAsync(updatedTaskNote.Text, ProcessId);

            Assert.AreSame("Test task note for update",
                _dbContext.TaskNote.FirstAsync(tn => tn.ProcessId == ProcessId).Result.Text);
        }

        [Test]
        public async Task Task_Note_Is_Updated_To_Blank_When_One_Already_Exists()
        {
            var taskNote = new TaskNote
            {
                ProcessId = ProcessId,
                WorkflowInstanceId = 1,
                Text = "Test task note",
                Created = DateTime.Now,
                CreatedByUsername = "Tests",
                LastModified = DateTime.Now,
                LastModifiedByUsername = "Tests"
            };

            var updatedTaskNote = new TaskNote
            {
                ProcessId = ProcessId,
                WorkflowInstanceId = 1,
                Text = null,
                LastModified = DateTime.Now,
                LastModifiedByUsername = "Tests"
            };

            await _dbContext.TaskNote.AddAsync(taskNote);
            await _dbContext.SaveChangesAsync();

            var indexModel = new IndexModel(_dbContext,
                _fakeMapper,
                _fakeUserIdentityService);

            await indexModel.OnPostTaskNoteAsync(updatedTaskNote.Text, ProcessId);

            Assert.AreSame(string.Empty,
                _dbContext.TaskNote.FirstAsync(tn => tn.ProcessId == ProcessId).Result.Text);
        }
    }
}
