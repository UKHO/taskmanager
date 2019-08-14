using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NServiceBus.Testing;
using NUnit.Framework;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using DataServices.Models;
using WorkflowCoordinator.Sagas;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.UnitTests
{
    public class AssessmentPollingSagaTests
    {
        private AssessmentPollingSaga _saga;
        private IDataServiceApiClient _fakeDataServiceApiClient;
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
            _saga = new AssessmentPollingSaga(generalConfigOptionsSnapshot,
                _fakeDataServiceApiClient, _dbContext)
            { Data = new AssessmentPollingSagaData() };
            _handlerContext = new TestableMessageHandlerContext();
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
        public async Task Test_sends_new_InitiateSourceDocumentRetrievalCommand_to_SourceDocumentCoordinator()
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
            var initiateRetrievalCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                     t.Message is InitiateSourceDocumentRetrievalCommand);
            Assert.IsNotNull(initiateRetrievalCommand, $"No message of type {nameof(InitiateSourceDocumentRetrievalCommand)} seen.");
        }

        [Test]
        public async Task Test_sends_three_messages()
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
            Assert.AreEqual(3, _handlerContext.SentMessages.Length);
        }

        [Test]
        public async Task Test_if_all_assessment_match_database_rows_then_only_timeout_fired()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB")).Returns(Task.FromResult<IEnumerable<DocumentObject>>(new List<DocumentObject>()
            {
                new DocumentObject()
                {
                    Id = 1,
                    SourceName = "1234",
                    Name = "name"
                }
            }));

            await _dbContext.AssessmentData.AddAsync(new WorkflowDatabase.EF.Models.AssessmentData()
            {
                RsdraNumber = "1234"
            });
            await _dbContext.SaveChangesAsync();

            //When
            await _saga.Timeout(new ExecuteAssessmentPollingTask(), _handlerContext).ConfigureAwait(false);

            //Then
            Assert.AreEqual(1, _handlerContext.SentMessages.Length);
            var executeAssessmentPollingTask = _handlerContext.SentMessages.SingleOrDefault(t =>
                t.Message is ExecuteAssessmentPollingTask);
            Assert.IsNotNull(executeAssessmentPollingTask, $"No timeout of type {nameof(ExecuteAssessmentPollingTask)} seen.");
        }
    }
}