using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using WorkflowCoordinator.Handlers;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Models;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.UnitTests
{
    public class SdraPollingMessageHandlerTests
    {
        private SdraPollingMessageHandler _handler;
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

            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _handler = new SdraPollingMessageHandler(_fakeDataServiceApiClient, dbContext);
            _handlerContext = new TestableMessageHandlerContext();
        }

        [Test]
        public async Task Test_call_getassessments_exactly_once()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                                                    .Returns(Task.FromResult<IEnumerable<AssessmentModel>>(A.Dummy<IEnumerable<AssessmentModel>>()));

            //When
            await _handler.Handle(new SdraPollingMessage(), _handlerContext);

            //Then
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Test_sends_new_sdrapollingmessage_with_delayed_send_five_seconds()
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
            await _handler.Handle(new SdraPollingMessage(), _handlerContext).ConfigureAwait(false);

            //Then
            Assert.IsNotNull(_handlerContext.SentMessages);

            var sdraPollingMessage = _handlerContext.SentMessages.SingleOrDefault(t =>
                     t.Message is SdraPollingMessage);
            Assert.IsNotNull(sdraPollingMessage, $"No message of type {nameof(SdraPollingMessage)} seen.");

            Assert.IsTrue(sdraPollingMessage.Options.IsRoutingToThisEndpoint());
            Assert.AreEqual(TimeSpan.FromSeconds(5), sdraPollingMessage.Options.GetDeliveryDelay());
        }

        [Test]
        public async Task Test_sends_new_StartDbAssessmentCommand()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB"))
                .Returns(
                    Task.FromResult<IEnumerable<AssessmentModel>>(new List<AssessmentModel>()
                    {
                        new AssessmentModel()
                        {
                            SdocId = 1,
                            RsdraNumber = "sourcename",
                            Name = "name"
                        }
                    }));

            //When
            await _handler.Handle(new SdraPollingMessage(), _handlerContext).ConfigureAwait(false);

            //Then
            Assert.IsNotNull(_handlerContext.SentMessages);

            var startDbAssessmentCommand = _handlerContext.SentMessages.SingleOrDefault(t =>
                     t.Message is StartDbAssessmentCommand);
            Assert.IsNotNull(startDbAssessmentCommand, $"No message of type {nameof(StartDbAssessmentCommand)} seen.");

            Assert.IsTrue(startDbAssessmentCommand.Options.IsRoutingToThisEndpoint());
        }


        [Test]
        public async Task Test_sends_two_messages()
        {
            //Given
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB")).Returns(Task.FromResult<IEnumerable<AssessmentModel>>(new List<AssessmentModel>()
            {
                new AssessmentModel()
                {
                    SdocId = 1,
                    RsdraNumber = "sourcename",
                    Name = "name"
                }
            }));

            //When
            await _handler.Handle(new SdraPollingMessage(), _handlerContext).ConfigureAwait(false);

            //Then
            Assert.AreEqual(2, _handlerContext.SentMessages.Length);
        }
    }
}