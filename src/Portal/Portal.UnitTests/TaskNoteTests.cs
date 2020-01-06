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
using Portal.Helpers;
using Portal.MappingProfiles;
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
        private IMapper _mapper;
        private IUserIdentityService _fakeUserIdentityService;
        private IIndexFacade _fakeIndexFacade;

        [SetUp]
        public async Task Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();

            var mappingConfig = new MapperConfiguration(mc => { mc.AddProfile(new TaskViewModelMappingProfile()); });
            _mapper = mappingConfig.CreateMapper();

            _fakeUserIdentityService = A.Fake<IUserIdentityService>();
            _fakeIndexFacade = A.Fake<IIndexFacade>();

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
                WorkflowType = "DbAssessment",
                TaskNote = new TaskNote(),
                DataImpact = new List<DataImpact>(),
                DatabaseDocumentStatus = new List<DatabaseDocumentStatus>(),
                DbAssessmentAssessData = new DbAssessmentAssessData(),
                DbAssessmentAssignTask = new List<DbAssessmentAssignTask>(),
                LinkedDocument = new List<LinkedDocument>(),
                PrimaryDocumentStatus = new PrimaryDocumentStatus(),
                ProductAction = new List<ProductAction>(),
                ParentProcessId = null
            });

            _dbContext.AssessmentData.Add(new AssessmentData
            {
                ProcessId = ProcessId,
                SourceDocumentName = "TestName",
                ReceiptDate = DateTime.Now,
                SourceDocumentType = "Primary",
                RsdraNumber = "RSDRA1234567",
                EffectiveStartDate = DateTime.Now,
                Datum = "12345",
                TeamDistributedTo = "HW",
                PrimarySdocId = 12345,
                SourceNature = "Outside"
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
                _mapper,
                _fakeUserIdentityService,
                _fakeIndexFacade);

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
                _mapper,
                _fakeUserIdentityService,
                _fakeIndexFacade);

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
                _mapper,
                _fakeUserIdentityService,
                _fakeIndexFacade);

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
                _mapper,
                _fakeUserIdentityService,
                _fakeIndexFacade);

            await indexModel.OnPostTaskNoteAsync(updatedTaskNote.Text, ProcessId);

            Assert.AreSame(string.Empty,
                _dbContext.TaskNote.FirstAsync(tn => tn.ProcessId == ProcessId).Result.Text);
        }
    }
}
