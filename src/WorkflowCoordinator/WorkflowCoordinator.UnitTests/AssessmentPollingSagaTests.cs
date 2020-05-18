using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataServices.Models;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus.Testing;
using NUnit.Framework;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Sagas;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace WorkflowCoordinator.UnitTests
{
    public class AssessmentPollingSagaTests
    {
        private AssessmentPollingSaga _saga;
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private ILogger<AssessmentPollingSaga> _fakeLogger;
        private TestableMessageHandlerContext _handlerContext;
        private WorkflowDbContext _dbContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseInMemoryDatabase(databaseName: "inmemory")
                .Options;

            _dbContext = new WorkflowDbContext(dbContextOptions);

            var generalConfigOptionsSnapshot = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            var generalConfig = new GeneralConfig { WorkflowCoordinatorAssessmentPollingIntervalSeconds = 5, CallerCode = "HDB" };
            A.CallTo(() => generalConfigOptionsSnapshot.Value).Returns(generalConfig);

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _fakeLogger = A.Dummy<ILogger<AssessmentPollingSaga>>();
            _saga = new AssessmentPollingSaga(generalConfigOptionsSnapshot,
                _fakeDataServiceApiClient,
                _dbContext,
                _fakeLogger)
            { Data = new AssessmentPollingSagaData() };
            _handlerContext = new TestableMessageHandlerContext();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
        }

        [Test]
        public async Task Test_startassessmentpolling_requests_timeout()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                .Returns(Task.FromResult<IEnumerable<DocumentObject>>(A.Dummy<IEnumerable<DocumentObject>>()));

            //When
            await _saga.Handle(new StartAssessmentPollingCommand(Guid.NewGuid()), _handlerContext);

            //Then
            var executeAssessmentPollingTask = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                t.Message is ExecuteAssessmentPollingTask);
            Assert.IsNotNull(executeAssessmentPollingTask, $"No timeout of type {nameof(ExecuteAssessmentPollingTask)} seen.");
        }

        [Test]
        public async Task Test_startassessmentpolling_does_not_request_timeout()
        {
            //Given
            _saga.Data = new AssessmentPollingSagaData { IsTaskAlreadyScheduled = true };
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                .Returns(Task.FromResult<IEnumerable<DocumentObject>>(A.Dummy<IEnumerable<DocumentObject>>()));

            //When
            await _saga.Handle(new StartAssessmentPollingCommand(Guid.NewGuid()), _handlerContext);

            //Then
            Assert.IsEmpty(_handlerContext.TimeoutMessages);
        }


        [Test]
        public async Task Test_call_getassessments_exactly_once()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                                                    .Returns(Task.FromResult<IEnumerable<DocumentObject>>(A.Dummy<IEnumerable<DocumentObject>>()));

            //When
            await _saga.Timeout(new ExecuteAssessmentPollingTask(), _handlerContext);

            //Then
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Test_requests_new_timeout()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                .Returns(Task.FromResult<IEnumerable<DocumentObject>>(new List<DocumentObject>()
                {
                    new DocumentObject()
                    {
                        Id = 1,
                        SourceName = "sourcename",
                        Name = "name"
                    }
                }));

            //When
            await _saga.Timeout(new ExecuteAssessmentPollingTask(), _handlerContext).ConfigureAwait(false);

            //Then
            var executeAssessmentPollingTask = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                     t.Message is ExecuteAssessmentPollingTask);
            Assert.IsNotNull(executeAssessmentPollingTask, $"No timeout of type {nameof(ExecuteAssessmentPollingTask)} seen.");
        }

        [Test]
        public async Task Test_sends_new_StartDbAssessmentCommand()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                .Returns(
                    Task.FromResult<IEnumerable<DocumentObject>>(new List<DocumentObject>()
                    {
                        new DocumentObject()
                        {
                            Id = 1,
                            SourceName = "sourcename",
                            Name = "name"
                        }
                    }));

            //When
            await _saga.Timeout(new ExecuteAssessmentPollingTask(), _handlerContext).ConfigureAwait(false);

            //Then
            var startDbAssessmentCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                     t.Message is StartDbAssessmentCommand);
            Assert.IsNotNull(startDbAssessmentCommand, $"No message of type {nameof(StartDbAssessmentCommand)} seen.");
        }

        [Test]
        public async Task Test_sends_two_messages()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB")).Returns(Task.FromResult<IEnumerable<DocumentObject>>(new List<DocumentObject>()
            {
                new DocumentObject()
                {
                    Id = 1,
                    SourceName = "sourcename",
                    Name = "name"
                }
            }));

            //When
            await _saga.Timeout(new ExecuteAssessmentPollingTask(), _handlerContext).ConfigureAwait(false);

            //Then
            Assert.AreEqual(2, _handlerContext.SentMessages.Length);
        }

        [Test]
        public async Task Test_if_all_assessment_match_database_rows_then_only_timeout_fired()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB")).Returns(Task.FromResult<IEnumerable<DocumentObject>>(new List<DocumentObject>()
            {
                new DocumentObject()
                {
                    Id = 1888403,
                    Name = "SURVEY_CORRESP_HI1530_GRENADA_ST_VINCENT_AND_GRENADINES_LIDAR_26-05-17",
                    SourceName = "RSDRA2017000130865"
                }
            }));

            await _dbContext.AssessmentData.AddAsync(new WorkflowDatabase.EF.Models.AssessmentData()
            {
                PrimarySdocId = 1888403,
                RsdraNumber = "RSDRA2017000130865"
            });
            await _dbContext.SaveChangesAsync();

            //When
            await _saga.Timeout(new ExecuteAssessmentPollingTask(), _handlerContext).ConfigureAwait(false);

            //Then
            Assert.GreaterOrEqual(_handlerContext.SentMessages.Length, 1);
            var executeAssessmentPollingTask = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is ExecuteAssessmentPollingTask);
            Assert.IsNotNull(executeAssessmentPollingTask, $"No timeout of type {nameof(ExecuteAssessmentPollingTask)} seen.");
        }


        [Test]
        public async Task Test_ExecuteAssessmentPollingTask_checks_with_WorkflowInstance_table_for_processed_sdocs_and_only_sends_StartDbAssessmentCommand_for_unprocessed_sdoc()
        {
            //Given

            var primarySdocIdAlreadyProcessed = 1111;
            var primarySdocIdNotProcessed = 2222;

            await _dbContext.WorkflowInstance.AddAsync(new WorkflowInstance()
            {
                ProcessId = 1,
                PrimarySdocId = primarySdocIdAlreadyProcessed,
                ActivityName = WorkflowStage.Review.ToString(),
                ActivityChangedAt = DateTime.Today
            });

            await _dbContext.SaveChangesAsync();
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                .Returns( new List<DocumentObject>()
                    {
                        new DocumentObject()
                        {
                            Id = primarySdocIdAlreadyProcessed,
                            SourceName = "sourcename1",
                            Name = "name1"
                        },
                        new DocumentObject()
                        {
                            Id = primarySdocIdNotProcessed,
                            SourceName = "sourcename2",
                            Name = "name2"
                        }

                    });

            //When
            await _saga.Timeout(new ExecuteAssessmentPollingTask(), _handlerContext).ConfigureAwait(false);

            //Then
            var messages = _handlerContext.SentMessages;

            Assert.AreEqual(2, messages.Length);

            var startDbAssessmentCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is StartDbAssessmentCommand);
            Assert.IsNotNull(startDbAssessmentCommand, $"No message of type {nameof(StartDbAssessmentCommand)} seen.");

            Assert.AreEqual(primarySdocIdNotProcessed,((StartDbAssessmentCommand) startDbAssessmentCommand.Message).SourceDocumentId);
        }
    }
}