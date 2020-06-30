using Common.Helpers.Auth;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using NCNEPortal.Models;
using NCNEPortal.Pages;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NCNEPortal.UnitTests
{
    public class HistoricalTasksTests
    {
        private NcneWorkflowDbContext _dbContext;
        private HistoricalTasksModel _historicalTasksModel;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;
        private IAdDirectoryService _fakeAdDirectoryService;
        private ILogger<HistoricalTasksModel> _fakeLogger;


        [SetUp]
        public void SetUp()
        {
            var dbContextOptions = new DbContextOptionsBuilder<NcneWorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;
            _dbContext = new NcneWorkflowDbContext(dbContextOptions);

            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakeLogger = A.Fake<ILogger<HistoricalTasksModel>>();

            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            _historicalTasksModel =
                new HistoricalTasksModel(_dbContext, _fakeAdDirectoryService, _generalConfig, _fakeLogger);

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
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 01, 01).Date,
                ChartNumber = "100",
                ChartType = "Primary",
                WorkflowType = "NE"
            };

            var completedTask = new TaskInfo()
            {
                ProcessId = 4,
                Status = NcneTaskStatus.Completed.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 01, 01).Date,
                ChartNumber = "200",
                ChartType = "Primary",
                WorkflowType = "NC"
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask,
                new TaskInfo()
                {
                    ProcessId = 1,
                    Status = NcneTaskStatus.InProgress.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 01, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NC"
                },
                completedTask,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = NcneTaskStatus.InProgress.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 01, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NC"
                }

            });

            await _dbContext.SaveChangesAsync();

            var filteredTasks = new List<TaskInfo> { completedTask, terminatedTask };


            await _historicalTasksModel.OnGetAsync();

            Assert.AreEqual(2, _historicalTasksModel.NcneTasks.Count);
            Assert.IsTrue(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 2 || h.ProcessId == 4));
            Assert.IsFalse(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 3));


        }



        [Test]
        public async Task
            Test_OnGet_returns_Latest_Terminated_or_Completed_Tasks_that_is_less_or_equal_configured_count()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            var terminatedTask1 = new TaskInfo()
            {
                ProcessId = 1,
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 05, 01).Date,
                ChartNumber = "100",
                ChartType = "Primary",
                WorkflowType = "NE"
            };

            var terminatedTask2 = new TaskInfo()
            {
                ProcessId = 2,
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 04, 01).Date,
                ChartNumber = "200",
                ChartType = "Primary",
                WorkflowType = "NC"
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask1,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = NcneTaskStatus.Completed.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 03, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NC"
                },
                terminatedTask2,
                new TaskInfo()
                {
                    ProcessId = 4,
                    Status = NcneTaskStatus.Completed.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 02, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NC"
                }

            });

            await _dbContext.SaveChangesAsync();

            await _historicalTasksModel.OnGetAsync();

            Assert.AreEqual(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords,
                _historicalTasksModel.NcneTasks.Count);
            Assert.IsTrue(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 4));

        }

        [Test]
        public async Task
            Test_OnPost_when_empty_search_parameters_returns_Latest_Terminated_or_Completed_Tasks_that_is_less_or_equal_configured_count()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            var terminatedTask1 = new TaskInfo()
            {
                ProcessId = 1,
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 05, 01).Date,
                ChartNumber = "100",
                ChartType = "Primary",
                WorkflowType = "NE"
            };

            var terminatedTask2 = new TaskInfo()
            {
                ProcessId = 2,
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 04, 01).Date,
                ChartNumber = "200",
                ChartType = "Primary",
                WorkflowType = "NC"
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask1,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = NcneTaskStatus.Completed.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 03, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NC"
                },
                terminatedTask2,
                new TaskInfo()
                {
                    ProcessId = 4,
                    Status = NcneTaskStatus.Completed.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 02, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NC"
                }

            });

            await _dbContext.SaveChangesAsync();

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters();

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords,
                _historicalTasksModel.NcneTasks.Count);
            Assert.IsTrue(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 4));

        }



        [Test]
        public async Task
            Test_OnPost_when_Chart_Details_in_search_parameters_are_populated_returns_Latest_Terminated_or_Completed_Tasks_that_fulfill_search_criteria()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 4;

            var terminatedTask1 = new TaskInfo()
            {
                ProcessId = 1,
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 05, 01).Date,
                ChartNumber = "100",
                ChartType = "Primary",
                WorkflowType = "NE"
            };

            var terminatedTask2 = new TaskInfo()
            {
                ProcessId = 2,
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 04, 01).Date,
                ChartNumber = "200",
                ChartType = "Primary",
                WorkflowType = "NC"
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask1,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = NcneTaskStatus.Completed.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 03, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NE"
                },
                terminatedTask2,
                new TaskInfo()
                {
                    ProcessId = 4,
                    Status = NcneTaskStatus.Completed.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 02, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NC"
                }

            });

            await _dbContext.SaveChangesAsync();

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters()
            {
                WorkflowType = "NC"
            };

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(2,
                _historicalTasksModel.NcneTasks.Count);
            Assert.IsTrue(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 4 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 1));

        }

        [Test]
        public async Task
            Test_OnPost_when_Users_in_search_parameters_are_populated_returns_Latest_Terminated_or_Completed_Tasks_that_fulfill_search_criteria()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 4;

            var terminatedTask1 = new TaskInfo()
            {
                ProcessId = 1,
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 05, 01).Date,
                ChartNumber = "100",
                ChartType = "Primary",
                WorkflowType = "NE",
                TaskRole = new TaskRole()
                {
                    Compiler = "Peter Bates",
                    VerifierOne = "Matthew Stoodley",
                    VerifierTwo = "Gareth Evans",
                    HundredPercentCheck = "Greg Williams"
                }
            };

            var terminatedTask2 = new TaskInfo()
            {
                ProcessId = 2,
                Status = NcneTaskStatus.Terminated.ToString(),
                AssignedUser = "Valid User",
                AssignedDate = new DateTime(2020, 01, 01),
                StatusChangeDate = new DateTime(2020, 04, 01).Date,
                ChartNumber = "200",
                ChartType = "Primary",
                WorkflowType = "NC",
                TaskRole = new TaskRole()
                {
                    Compiler = "Peter Bates",
                    VerifierOne = "Matthew Stoodley",
                    VerifierTwo = "Gareth Evans",
                    HundredPercentCheck = "Greg Williams"
                }
            };

            await _dbContext.TaskInfo.AddRangeAsync(new List<TaskInfo>()
            {
                terminatedTask1,
                new TaskInfo()
                {
                    ProcessId = 3,
                    Status = NcneTaskStatus.Completed.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 03, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NE",
                    TaskRole = new TaskRole()
                    {
                        Compiler = "Rajan Shunmuga",
                        VerifierOne = "Rossall Sandford",
                        HundredPercentCheck = "Samir Hassoun"
                    }
                },
                terminatedTask2,
                new TaskInfo()
                {
                    ProcessId = 4,
                    Status = NcneTaskStatus.Completed.ToString(),
                    AssignedUser = "Valid User",
                    AssignedDate = new DateTime(2020, 01, 01),
                    StatusChangeDate = new DateTime(2020, 02, 01).Date,
                    ChartNumber = "200",
                    ChartType = "Primary",
                    WorkflowType = "NC",
                    TaskRole = new TaskRole()
                    {   Compiler = "Rajan Shunmuga",
                        VerifierOne = "Rossall Sandford",
                        HundredPercentCheck = "Samir Hassoun"
                    }
                }

            });

            await _dbContext.SaveChangesAsync();

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters()
            {
                Compiler = "Pet",
                VerifierOne = "Matt",
                VerifierTwo = "G",
                HundredPercentCheck = "Greg"

            };

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(2,
                _historicalTasksModel.NcneTasks.Count);
            Assert.IsTrue(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.NcneTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 4));

        }
    }
}
