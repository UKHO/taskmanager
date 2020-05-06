using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Common.Helpers.Auth;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Portal.Calculators;
using Portal.Configuration;
using Portal.Models;
using Portal.Pages.DbAssessment;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.UnitTests
{
    public class HistoricalTasksTests
    {
        private WorkflowDbContext _dbContext;
        private HistoricalTasksModel _historicalTasksModel;
        private IDmEndDateCalculator _dmEndDateCalculator;
        private IMapper _mapper;
        private IOptionsSnapshot<GeneralConfig> _generalConfig;
        private IAdDirectoryService _fakeAdDirectoryService;
        private ILogger<HistoricalTasksModel> _fakeLogger;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;
            _dbContext = new WorkflowDbContext(dbContextOptions);

            _dmEndDateCalculator = A.Fake<IDmEndDateCalculator>();
            _mapper = A.Fake<IMapper>();
            _fakeAdDirectoryService = A.Fake<IAdDirectoryService>();
            _fakeLogger = A.Fake<ILogger<HistoricalTasksModel>>();

            _generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            _historicalTasksModel = new HistoricalTasksModel(_dbContext, _dmEndDateCalculator, _mapper, _generalConfig, _fakeAdDirectoryService, _fakeLogger);

        }


        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_OnGet_returns_Terminated_or_Completed_Tasks()
        {
            var terminatedTask = new WorkflowInstance()
            {
                WorkflowInstanceId = 2,
                ProcessId = 2,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 01, 01).Date,
                AssessmentData = new AssessmentData(),
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
            };

            var completedTasks = new WorkflowInstance()
            {
                WorkflowInstanceId = 4,
                ProcessId = 4,
                ActivityName = WorkflowStage.Verify.ToString(),
                Status = WorkflowStatus.Completed.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 01, 01).Date,
                AssessmentData = new AssessmentData(),
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
            };

            await _dbContext.WorkflowInstance.AddRangeAsync(new List<WorkflowInstance>(){
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 1,
                                                ProcessId = 1,
                                                ActivityName = WorkflowStage.Review.ToString(),
                                                Status = WorkflowStatus.Started.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 01, 01).Date
                                            },
                                            terminatedTask,
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 3,
                                                ProcessId = 3,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Started.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 01, 01).Date
                                            },
                                            completedTasks,
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 5,
                                                ProcessId = 5,
                                                ActivityName = WorkflowStage.Assess.ToString(),
                                                Status = WorkflowStatus.Started.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 01, 01).Date
                                            }
            });


            await _dbContext.SaveChangesAsync();

            var filteredWorkflows = new List<WorkflowInstance>();
            filteredWorkflows.Add(terminatedTask);
            filteredWorkflows.Add(completedTasks);

            var historicalTasks = new List<HistoricalTasksData>()
            {
                new HistoricalTasksData()
                {
                    ProcessId = terminatedTask.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(terminatedTask.ActivityName),
                    Status = Enum.Parse<WorkflowStatus>(terminatedTask.Status),
                    ActivityChangedAt = terminatedTask.ActivityChangedAt
                },
                new HistoricalTasksData()
                {
                    ProcessId = completedTasks.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(completedTasks.ActivityName),
                    Status = Enum.Parse<WorkflowStatus>(completedTasks.Status),
                    ActivityChangedAt = completedTasks.ActivityChangedAt

                }
            };

            A.CallTo(() => _mapper.Map<List<WorkflowInstance>, List<HistoricalTasksData>>(A<List<WorkflowInstance>>.That.IsSameSequenceAs(filteredWorkflows)))
                .Returns(historicalTasks);

            await _historicalTasksModel.OnGetAsync();

            Assert.AreEqual(2, _historicalTasksModel.HistoricalTasks.Count);
            Assert.IsTrue(_historicalTasksModel.HistoricalTasks.Any(h => h.ProcessId == 2 || h.ProcessId == 4));
            Assert.IsFalse(_historicalTasksModel.HistoricalTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 3 || h.ProcessId == 5));
        }


        [Test]
        public async Task Test_OnGet_returns_Latest_Terminated_or_Completed_Tasks_that_is_less_or_equal_configured_count()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            var returnedWorkflow1 = new WorkflowInstance()
            {
                WorkflowInstanceId = 1,
                ProcessId = 1,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 05, 01).Date,
                AssessmentData = new AssessmentData(),
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
            };

            var returnedWorkflow2 = new WorkflowInstance()
            {
                WorkflowInstanceId = 2,
                ProcessId = 2,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 04, 01).Date,
                AssessmentData = new AssessmentData(),
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
            };

            await _dbContext.WorkflowInstance.AddRangeAsync(new List<WorkflowInstance>(){
                                            returnedWorkflow1,
                                            returnedWorkflow2,
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 3,
                                                ProcessId = 3,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Completed.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 03, 01).Date,
                                                AssessmentData = new AssessmentData(),
                                                DbAssessmentReviewData = new DbAssessmentReviewData(),
                                                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                                            },
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 4,
                                                ProcessId = 4,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Completed.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 02, 01).Date,
                                                AssessmentData = new AssessmentData(),
                                                DbAssessmentReviewData = new DbAssessmentReviewData(),
                                                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                                            }
            });


            await _dbContext.SaveChangesAsync();

            var filteredWorkflows = new List<WorkflowInstance>();
            filteredWorkflows.Add(returnedWorkflow1);
            filteredWorkflows.Add(returnedWorkflow2);

            var historicalTasks = new List<HistoricalTasksData>()
            {
                new HistoricalTasksData()
                {
                    ProcessId = returnedWorkflow1.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(returnedWorkflow1.ActivityName),
                    Status = Enum.Parse<WorkflowStatus>(returnedWorkflow1.Status),
                    ActivityChangedAt = returnedWorkflow1.ActivityChangedAt
                },
                new HistoricalTasksData()
                {
                    ProcessId = returnedWorkflow2.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(returnedWorkflow2.ActivityName),
                    Status = Enum.Parse<WorkflowStatus>(returnedWorkflow2.Status),
                    ActivityChangedAt = returnedWorkflow2.ActivityChangedAt

                }
            };

            A.CallTo(() => _mapper.Map<List<WorkflowInstance>, List<HistoricalTasksData>>(A<List<WorkflowInstance>>.That.IsSameSequenceAs(filteredWorkflows)))
                .Returns(historicalTasks);

            await _historicalTasksModel.OnGetAsync();

            Assert.AreEqual(_generalConfig.Value.HistoricalTasksInitialNumberOfRecords, _historicalTasksModel.HistoricalTasks.Count);
            Assert.IsTrue(_historicalTasksModel.HistoricalTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.HistoricalTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 4));
        }

        [Test]
        public async Task Test_OnPost_when_empty_search_parameters_returns_Latest_Terminated_or_Completed_Tasks_that_is_less_or_equal_configured_count()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 2;

            var returnedWorkflow1 = new WorkflowInstance()
            {
                WorkflowInstanceId = 1,
                ProcessId = 1,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 05, 01).Date,
                AssessmentData = new AssessmentData(),
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
            };

            var returnedWorkflow2 = new WorkflowInstance()
            {
                WorkflowInstanceId = 2,
                ProcessId = 2,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 04, 01).Date,
                AssessmentData = new AssessmentData(),
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
            };

            await _dbContext.WorkflowInstance.AddRangeAsync(new List<WorkflowInstance>(){
                                            returnedWorkflow1,
                                            returnedWorkflow2,
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 3,
                                                ProcessId = 3,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Completed.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 03, 01).Date,
                                                AssessmentData = new AssessmentData(),
                                                DbAssessmentReviewData = new DbAssessmentReviewData(),
                                                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                                            },
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 4,
                                                ProcessId = 4,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Completed.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 02, 01).Date,
                                                AssessmentData = new AssessmentData(),
                                                DbAssessmentReviewData = new DbAssessmentReviewData(),
                                                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                                            }
            });


            await _dbContext.SaveChangesAsync();

            var filteredWorkflows = new List<WorkflowInstance>();
            filteredWorkflows.Add(returnedWorkflow1);
            filteredWorkflows.Add(returnedWorkflow2);

            var historicalTasks = new List<HistoricalTasksData>()
            {
                new HistoricalTasksData()
                {
                    ProcessId = returnedWorkflow1.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(returnedWorkflow1.ActivityName),
                    Status = Enum.Parse<WorkflowStatus>(returnedWorkflow1.Status),
                    ActivityChangedAt = returnedWorkflow1.ActivityChangedAt
                },
                new HistoricalTasksData()
                {
                    ProcessId = returnedWorkflow2.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(returnedWorkflow2.ActivityName),
                    Status = Enum.Parse<WorkflowStatus>(returnedWorkflow2.Status),
                    ActivityChangedAt = returnedWorkflow2.ActivityChangedAt

                }
            };

            A.CallTo(() => _mapper.Map<List<WorkflowInstance>, List<HistoricalTasksData>>(A<List<WorkflowInstance>>.That.IsSameSequenceAs(filteredWorkflows)))
                .Returns(historicalTasks);

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters();

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(2, _historicalTasksModel.HistoricalTasks.Count);
            Assert.IsTrue(_historicalTasksModel.HistoricalTasks.Any(h => h.ProcessId == 1 || h.ProcessId == 2));
            Assert.IsFalse(_historicalTasksModel.HistoricalTasks.Any(h => h.ProcessId == 3 || h.ProcessId == 4));
        }

        [Test]
        public async Task Test_OnPost_when_assessmentData_in_search_parameters_are_populated_returns_Latest_Terminated_or_Completed_Tasks_that_fulfill_search_criteria()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 4;

            var returnedWorkflow1 = new WorkflowInstance()
            {
                WorkflowInstanceId = 1,
                ProcessId = 1,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 05, 01).Date,
                AssessmentData = new AssessmentData()
                {
                    PrimarySdocId = 111,
                    ProcessId = 1,
                    RsdraNumber = "RSDRA12",
                    SourceDocumentName = "SourceDocument11"
                },
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
            };

            var returnedWorkflow2 = new WorkflowInstance()
            {
                WorkflowInstanceId = 2,
                ProcessId = 2,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 04, 01).Date,
                AssessmentData = new AssessmentData()
                {
                    PrimarySdocId = 222,
                    ProcessId = 2,
                    RsdraNumber = "RSDRA13",
                    SourceDocumentName = "SourceDocument12"
                },
                DbAssessmentReviewData = new DbAssessmentReviewData(),
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
            };

            await _dbContext.WorkflowInstance.AddRangeAsync(new List<WorkflowInstance>(){
                                            returnedWorkflow1,
                                            returnedWorkflow2,
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 3,
                                                ProcessId = 3,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Completed.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 03, 01).Date,
                                                AssessmentData = new AssessmentData()
                                                {
                                                    PrimarySdocId = 333,
                                                    ProcessId = 3,
                                                    RsdraNumber = "RSDRA3",
                                                    SourceDocumentName = "SourceDocument3"
                                                },
                                                DbAssessmentReviewData = new DbAssessmentReviewData(),
                                                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                                            },
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 4,
                                                ProcessId = 4,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Completed.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 02, 01).Date,
                                                AssessmentData = new AssessmentData()
                                                {
                                                    PrimarySdocId = 444,
                                                    ProcessId = 4,
                                                    RsdraNumber = "RSDRA4",
                                                    SourceDocumentName = "SourceDocument4"
                                                },
                                                DbAssessmentReviewData = new DbAssessmentReviewData(),
                                                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                                            }
            });


            await _dbContext.SaveChangesAsync();

            var filteredWorkflows = new List<WorkflowInstance>();
            filteredWorkflows.Add(returnedWorkflow1);
            filteredWorkflows.Add(returnedWorkflow2);

            var historicalTasks = new List<HistoricalTasksData>()
            {
                new HistoricalTasksData()
                {
                    ProcessId = returnedWorkflow1.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(returnedWorkflow1.ActivityName),
                    RsdraNumber = returnedWorkflow1.AssessmentData.RsdraNumber,
                    SourceDocumentName = returnedWorkflow1.AssessmentData.SourceDocumentName,
                    Status = Enum.Parse<WorkflowStatus>(returnedWorkflow1.Status),
                    ActivityChangedAt = returnedWorkflow1.ActivityChangedAt
                },
                new HistoricalTasksData()
                {
                    ProcessId = returnedWorkflow2.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(returnedWorkflow2.ActivityName),
                    RsdraNumber = returnedWorkflow2.AssessmentData.RsdraNumber,
                    SourceDocumentName = returnedWorkflow2.AssessmentData.SourceDocumentName,
                    Status = Enum.Parse<WorkflowStatus>(returnedWorkflow2.Status),
                    ActivityChangedAt = returnedWorkflow2.ActivityChangedAt

                }
            };

            A.CallTo(() => _mapper.Map<List<WorkflowInstance>, List<HistoricalTasksData>>(A<List<WorkflowInstance>>.That.IsSameSequenceAs(filteredWorkflows)))
                .Returns(historicalTasks);

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters()
            {
                RsdraNumber = "RSDRA1",
                SourceDocumentName = "SourceDocument1"
            };

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(2, _historicalTasksModel.HistoricalTasks.Count);
            Assert.IsTrue(_historicalTasksModel.HistoricalTasks.Any(h => h.RsdraNumber.Contains(_historicalTasksModel.SearchParameters.RsdraNumber)));
            Assert.IsTrue(_historicalTasksModel.HistoricalTasks.Any(h => h.SourceDocumentName.Contains(_historicalTasksModel.SearchParameters.SourceDocumentName)));
        }

        [Test]
        public async Task Test_OnPost_when_Users_in_search_parameters_are_populated_returns_Latest_Terminated_or_Completed_Tasks_that_fulfill_search_criteria()
        {
            _generalConfig.Value.HistoricalTasksInitialNumberOfRecords = 4;

            var reviewer = "Matthew Stoodley";
            var assessor = "Peter Bates";
            var verifier = "Rossall Sandford";

            var returnedWorkflow1 = new WorkflowInstance()
            {
                WorkflowInstanceId = 1,
                ProcessId = 1,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 05, 01).Date,
                AssessmentData = new AssessmentData(),
                DbAssessmentReviewData = new DbAssessmentReviewData()
                {
                    Reviewer = reviewer,
                    Assessor = assessor,
                    Verifier = verifier
                },
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                {
                    Reviewer = reviewer,
                    Assessor = assessor,
                    Verifier = verifier
                }
            };

            var returnedWorkflow2 = new WorkflowInstance()
            {
                WorkflowInstanceId = 2,
                ProcessId = 2,
                ActivityName = WorkflowStage.Review.ToString(),
                Status = WorkflowStatus.Terminated.ToString(),
                StartedAt = new DateTime(2020, 01, 01),
                ActivityChangedAt = new DateTime(2020, 04, 01).Date,
                AssessmentData = new AssessmentData(),
                DbAssessmentReviewData = new DbAssessmentReviewData()
                {
                    Reviewer = "Matthew Stoodley",
                    Assessor = "Peter Bates",
                    Verifier = "Rossall Sandford"
                },
                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                {
                    Reviewer = "Matthew Stoodley",
                    Assessor = "Peter Bates",
                    Verifier = "Rossall Sandford"
                }
            };

            await _dbContext.WorkflowInstance.AddRangeAsync(new List<WorkflowInstance>(){
                                            returnedWorkflow1,
                                            returnedWorkflow2,
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 3,
                                                ProcessId = 3,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Completed.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 03, 01).Date,
                                                AssessmentData = new AssessmentData(),
                                                DbAssessmentReviewData = new DbAssessmentReviewData()
                                                {
                                                    Reviewer = "Greg Williams",
                                                    Assessor = "Stuart Barzey",
                                                    Verifier = "Ben Hall"
                                                },
                                                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                                                {
                                                    Reviewer = "Greg Williams",
                                                    Assessor = "Stuart Barzey",
                                                    Verifier = "Ben Hall"
                                                }
                                            },
                                            new WorkflowInstance()
                                            {
                                                WorkflowInstanceId = 4,
                                                ProcessId = 4,
                                                ActivityName = WorkflowStage.Verify.ToString(),
                                                Status = WorkflowStatus.Completed.ToString(),
                                                StartedAt = new DateTime(2020, 01, 01),
                                                ActivityChangedAt = new DateTime(2020, 02, 01).Date,
                                                AssessmentData = new AssessmentData(),
                                                DbAssessmentReviewData = new DbAssessmentReviewData()
                                                {
                                                    Reviewer = "Greg Williams",
                                                    Assessor = "Stuart Barzey",
                                                    Verifier = "Ben Hall"
                                                },
                                                DbAssessmentVerifyData = new DbAssessmentVerifyData()
                                                {
                                                    Reviewer = "Greg Williams",
                                                    Assessor = "Stuart Barzey",
                                                    Verifier = "Ben Hall"
                                                }
                                            }
            });


            await _dbContext.SaveChangesAsync();

            var filteredWorkflows = new List<WorkflowInstance>();
            filteredWorkflows.Add(returnedWorkflow1);
            filteredWorkflows.Add(returnedWorkflow2);

            var historicalTasks = new List<HistoricalTasksData>()
            {
                new HistoricalTasksData()
                {
                    ProcessId = returnedWorkflow1.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(returnedWorkflow1.ActivityName),
                    RsdraNumber = returnedWorkflow1.AssessmentData.RsdraNumber,
                    SourceDocumentName = returnedWorkflow1.AssessmentData.SourceDocumentName,
                    Status = Enum.Parse<WorkflowStatus>(returnedWorkflow1.Status),
                    ActivityChangedAt = returnedWorkflow1.ActivityChangedAt,
                    Reviewer = reviewer,
                    Assessor = assessor,
                    Verifier = verifier
                },
                new HistoricalTasksData()
                {
                    ProcessId = returnedWorkflow2.ProcessId,
                    TaskStage = Enum.Parse<WorkflowStage>(returnedWorkflow2.ActivityName),
                    RsdraNumber = returnedWorkflow2.AssessmentData.RsdraNumber,
                    SourceDocumentName = returnedWorkflow2.AssessmentData.SourceDocumentName,
                    Status = Enum.Parse<WorkflowStatus>(returnedWorkflow2.Status),
                    ActivityChangedAt = returnedWorkflow2.ActivityChangedAt,
                    Reviewer = reviewer,
                    Assessor = assessor,
                    Verifier = verifier

                }
            };

            A.CallTo(() => _mapper.Map<List<WorkflowInstance>, List<HistoricalTasksData>>(A<List<WorkflowInstance>>.That.IsSameSequenceAs(filteredWorkflows)))
                .Returns(historicalTasks);

            _historicalTasksModel.SearchParameters = new HistoricalTasksSearchParameters()
            {
                Reviewer = "Mat",
                Assessor = "Bates",
                Verifier = "Ross"
            };

            await _historicalTasksModel.OnPostAsync();

            Assert.AreEqual(2, _historicalTasksModel.HistoricalTasks.Count);
            Assert.IsTrue(_historicalTasksModel.HistoricalTasks.Any(h => h.Reviewer.Contains(reviewer)));
            Assert.IsTrue(_historicalTasksModel.HistoricalTasks.Any(h => h.Assessor.Contains(assessor)));
            Assert.IsTrue(_historicalTasksModel.HistoricalTasks.Any(h => h.Verifier.Contains(verifier)));
        }
    }
}
