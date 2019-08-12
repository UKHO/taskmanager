using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Messages.Commands;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using WorkflowCoordinator.Config;
using WorkflowCoordinator.Handlers;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Models;
using WorkflowCoordinator.Sagas;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.UnitTests
{
    public class AssessmentPollingSagaTests
    {
        private AssessmentPollingSaga _saga;
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private TestableMessageHandlerContext _handlerContext;

        [SetUp]
        public void Setup()
        {
            var dbContextOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
                .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=WorkflowDatabase;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False")
                .Options;
            var dbContext = new WorkflowDbContext(dbContextOptions);
            TasksDbBuilder.UsingDbContext(dbContext)
                .PopulateTables()
                .SaveChanges();
            var generalConfigOptionsSnapshot =  A.Fake<IOptionsSnapshot<GeneralConfig>>();
            var generalConfig = new GeneralConfig {WorkflowCoordinatorAssessmentPollingIntervalSeconds = 5};
            A.CallTo(() => generalConfigOptionsSnapshot.Value).Returns(generalConfig);

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _saga = new AssessmentPollingSaga(generalConfigOptionsSnapshot,
                _fakeDataServiceApiClient, dbContext) {Data = new AssessmentPollingSagaData() };
            _handlerContext = new TestableMessageHandlerContext();
        }

        [Test]
        public async Task Test_call_getassessments_exactly_once()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                                                    .Returns(Task.FromResult<IEnumerable<AssessmentModel>>(A.Dummy<IEnumerable<AssessmentModel>>()));

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
                .Returns(Task.FromResult<IEnumerable<AssessmentModel>>(new List<AssessmentModel>()
                {
                    new AssessmentModel()
                    {
                        SdocId = 1,
                        RsdraNumber = "sourcename",
                        Name = "name"
                    }
                }));

            //When
            await _saga.Timeout(new ExecuteAssessmentPollingTask(), _handlerContext).ConfigureAwait(false);

            //Then
            Assert.IsNotNull(_handlerContext.TimeoutMessages);

            var executeAssessmentPollingTask = _handlerContext.TimeoutMessages.SingleOrDefault(t =>
                     t.Message is ExecuteAssessmentPollingTask);
            Assert.IsNotNull(executeAssessmentPollingTask, $"No timeout of type {nameof(ExecuteAssessmentPollingTask)} seen.");
        }

        //[Test]
        //public async Task Test_sends_new_StartDbAssessmentCommand()
        //{
        //    //Given
        //    A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
        //        .Returns(
        //            Task.FromResult<IEnumerable<AssessmentModel>>(new List<AssessmentModel>()
        //            {
        //                new AssessmentModel()
        //                {
        //                    SdocId = 1,
        //                    RsdraNumber = "sourcename",
        //                    Name = "name"
        //                }
        //            }));

        //    //When
        //    await _saga.Timeout(new OpenAssessmentPollingMessage(), _handlerContext).ConfigureAwait(false);

        //    //Then
        //    Assert.IsNotNull(_handlerContext.SentMessages);

        //    var startDbAssessmentCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
        //             t.Message is StartDbAssessmentCommand);
        //    Assert.IsNotNull(startDbAssessmentCommand, $"No message of type {nameof(StartDbAssessmentCommand)} seen.");

        //    Assert.IsTrue(startDbAssessmentCommand.Options.IsRoutingToThisEndpoint());
        //}

        //[Test]
        //public async Task Test_sends_new_InitiateSourceDocumentRetrievalCommand_to_SourceDocumentCoordinator()
        //{
        //    //Given
        //    A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
        //        .Returns(
        //            Task.FromResult<IEnumerable<AssessmentModel>>(new List<AssessmentModel>()
        //            {
        //                new AssessmentModel()
        //                {
        //                    SdocId = 1,
        //                    RsdraNumber = "sourcename",
        //                    Name = "name"
        //                }
        //            }));

        //    //When
        //    await _saga.Timeout(new OpenAssessmentPollingMessage(), _handlerContext).ConfigureAwait(false);

        //    //Then
        //    Assert.IsNotNull(_handlerContext.SentMessages);

        //    var initiateRetrievalCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
        //             t.Message is InitiateSourceDocumentRetrievalCommand);
        //    Assert.IsNotNull(initiateRetrievalCommand, $"No message of type {nameof(InitiateSourceDocumentRetrievalCommand)} seen.");

        //    Assert.AreEqual("SourceDocumentCoordinator",
        //        initiateRetrievalCommand.Options.GetDestination());
        //}

        //[Test]
        //public async Task Test_sends_three_messages()
        //{
        //    //Given
        //    A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB")).Returns(Task.FromResult<IEnumerable<AssessmentModel>>(new List<AssessmentModel>()
        //    {
        //        new AssessmentModel()
        //        {
        //            SdocId = 1,
        //            RsdraNumber = "sourcename",
        //            Name = "name"
        //        }
        //    }));

        //    //When
        //    await _saga.Timeout(new OpenAssessmentPollingMessage(), _handlerContext).ConfigureAwait(false);

        //    //Then
        //    Assert.AreEqual(3, _handlerContext.SentMessages.Length);
        //}
    }
}