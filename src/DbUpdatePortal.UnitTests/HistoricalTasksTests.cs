using Common.Helpers.Auth;
using DbUpdatePortal.Configuration;
using DbUpdatePortal.Enums;
using DbUpdatePortal.Models;
using DbUpdatePortal.Pages;
using DbUpdatePortal.UnitTests.Helper;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbUpdatePortal.UnitTests
{
    public class HistoricalTasksTests
    {
        private DbUpdateWorkflowDbContext _dbContext;
        private HistoricalTasksModel _historicalTasksModel;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;
        private IAdDirectoryService _fakeAdDirectoryService;
        private ILogger<HistoricalTasksModel> _fakeLogger;
        private AdUser testUser;

        [SetUp]
        public void SetUp()
        {
            var dbContextOptions = new DbContextOptionsBuilder<DbUpdateWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;
            _dbContext = new DbUpdateWorkflowDbContext(dbContextOptions);

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakeLogger = A.Fake<ILogger<HistoricalTasksModel>>();

            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            _historicalTasksModel =
                new HistoricalTasksModel(_dbContext, _fakeAdDirectoryService, _generalConfig, _fakeLogger);

            testUser = AdUserHelper.CreateTestUser(_dbContext);

        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_OnGet_returns_Terminated_or_Completed_Tasks()
        {
            var terminatedTask = new TaskInfo()
            {
                ProcessId = 2,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 01, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Steady State",
                Name = "Task2"
            };

            var completedTask = new TaskInfo()
            {
                ProcessId = 4,
                Status = DbUpdateTaskStatus.Completed.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 01, 01).Date,
                ChartingArea = "Home waters",
                UpdateType = "Steady State",
                Name = "Task3"
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask,
                new TaskInfo()
                {
                    ProcessId = 1,
                    Status = DbUpdateTaskStatus.InProgress.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 01, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Steady State",
                    Name = "Task2"
                },
                completedTask,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = DbUpdateTaskStatus.InProgress.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 01, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Steady State",
                    Name = "Task2"
                }

            });

            await _dbContext.SaveChangesAsync();

            var filteredTasks = new List<TaskInfo> { completedTask, terminatedTask };


            await _historicalTasksModel.OnGetAsync();

            Assert.AreEqual(2, _historicalTasksModel.DbUpdateTasks.Count);
            Assert.IsTrue(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 2 || h.ProcessId == 4));
            Assert.IsFalse(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 3));


        }



        [Test]
        public async Task
            Test_OnGet_returns_Latest_Terminated_or_Completed_Tasks_that_is_less_or_equal_configured_count()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            var terminatedTask1 = new TaskInfo()
            {
                ProcessId = 1,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 05, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Steady State",
                Name = "Task1"
            };

            var terminatedTask2 = new TaskInfo()
            {
                ProcessId = 2,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 04, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Update from Source",
                Name = "Task2"
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask1,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = DbUpdateTaskStatus.Completed.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 03, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Update from Source",
                    Name = "Task3"
                },
                terminatedTask2,
                new TaskInfo()
                {
                    ProcessId = 4,
                    Status = DbUpdateTaskStatus.Completed.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 02, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Update from Source",
                    Name = "Task4"
                }

            });

            await _dbContext.SaveChangesAsync();

            await _historicalTasksModel.OnGetAsync();

            Assert.AreEqual(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords,
                _historicalTasksModel.DbUpdateTasks.Count);
            Assert.IsTrue(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 4));

        }

        [Test]
        public async Task
            Test_OnPost_when_empty_search_parameters_returns_Latest_Terminated_or_Completed_Tasks_that_is_less_or_equal_configured_count()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            var terminatedTask1 = new TaskInfo()
            {
                ProcessId = 1,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 05, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Update from Source",
                Name = "Task4"
            };

            var terminatedTask2 = new TaskInfo()
            {
                ProcessId = 2,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 04, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Update from Source",
                Name = "Task4"
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask1,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = DbUpdateTaskStatus.Completed.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 03, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Update from Source",
                    Name = "Task4"
                },
                terminatedTask2,
                new TaskInfo()
                {
                    ProcessId = 4,
                    Status = DbUpdateTaskStatus.Completed.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 02, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Update from Source",
                    Name = "Task4"
                }

            });

            await _dbContext.SaveChangesAsync();

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters();

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords,
                _historicalTasksModel.DbUpdateTasks.Count);
            Assert.IsTrue(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 4));

        }



        [Test]
        public async Task
            Test_OnPost_when_Chart_Details_in_search_parameters_are_populated_returns_Latest_Terminated_or_Completed_Tasks_that_fulfill_search_criteria()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 4;

            var terminatedTask1 = new TaskInfo()
            {
                ProcessId = 1,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 05, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Update from Source",
                Name = "Task1"
            };

            var terminatedTask2 = new TaskInfo()
            {
                ProcessId = 2,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 04, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Steady State",
                Name = "Task2"
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask1,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = DbUpdateTaskStatus.Completed.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 03, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Update from Source",
                    Name = "Task3"
                },
                terminatedTask2,
                new TaskInfo()
                {
                    ProcessId = 4,
                    Status = DbUpdateTaskStatus.Completed.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 02, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Steady State",
                    Name = "Task4"
                }

            });

            await _dbContext.SaveChangesAsync();

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters()
            {
                UpdateType = "Steady State"
            };

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(2,
                _historicalTasksModel.DbUpdateTasks.Count);
            Assert.IsTrue(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 4 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 1));

        }

        [Test]
        public async Task
            Test_OnPost_when_Users_in_search_parameters_are_populated_returns_Latest_Terminated_or_Completed_Tasks_that_fulfill_search_criteria()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 4;

            var userPB = AdUserHelper.CreateTestUser(_dbContext, "Peter Bates", 1);
            var userMS = AdUserHelper.CreateTestUser(_dbContext, "Matthew Stoodley", 2);

            var terminatedTask1 = new TaskInfo()
            {
                ProcessId = 1,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 05, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Steady State",
                Name = "Task1",
                TaskRole = new TaskRole()
                {
                    Compiler = userPB,
                    Verifier = userMS
                }
            };


            var terminatedTask2 = new TaskInfo()
            {
                ProcessId = 2,
                Status = DbUpdateTaskStatus.Terminated.ToString(),
                Assigned = testUser,
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 04, 01).Date,
                ChartingArea = "Primary charting",
                UpdateType = "Steady State",
                Name = "Task2",
                TaskRole = new TaskRole()
                {
                    Compiler = userPB,
                    Verifier = userMS
                }
            };

            var userBH = AdUserHelper.CreateTestUser(_dbContext, "Ben Halls", 5);
            var userRS = AdUserHelper.CreateTestUser(_dbContext, "Rossall Sandford", 6);


            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask1,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = DbUpdateTaskStatus.Completed.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 03, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Steady State",
                    Name = "Task3",
                    TaskRole = new TaskRole()
                    {
                        Compiler = userBH,
                        Verifier = userRS
                    }
                },
                terminatedTask2,
                new TaskInfo()
                {
                    ProcessId = 4,
                    Status = DbUpdateTaskStatus.Completed.ToString(),
                    Assigned = testUser,
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 02, 01).Date,
                    ChartingArea = "Primary charting",
                    UpdateType = "Steady State",
                    Name = "Task4",
                    TaskRole = new TaskRole()
                    {    Compiler = userBH,
                        Verifier = userRS
                    }
                }

            });

            await _dbContext.SaveChangesAsync();

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters()
            {
                Compiler = "Pet",
                Verifier = "Matt"


            };

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(2,
                _historicalTasksModel.DbUpdateTasks.Count);
            Assert.IsTrue(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.DbUpdateTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 4));

        }
    }
}
