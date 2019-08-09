using FakeItEasy;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowCoordinator.Handlers;
using WorkflowCoordinator.HttpClients;
using WorkflowCoordinator.Messages;
using WorkflowCoordinator.Models;

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
            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _handler = new SdraPollingMessageHandler(_fakeDataServiceApiClient);
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
            A.CallTo(() => _fakeDataServiceApiClient.GetAssessments("HDB")).Returns(Task.FromResult<IEnumerable<AssessmentModel>>(new List<AssessmentModel>()
            {
                new AssessmentModel()
                {
                    Id = 1,
                    SourceName = "sourcename",
                    Name = "name"
                }
            }));

            //When
            await _handler.Handle(new SdraPollingMessage(), _handlerContext).ConfigureAwait(false);

            //Then
            Assert.AreEqual(1, _handlerContext.SentMessages.Length);

            object o = _handlerContext.SentMessages[0].Message;
            Assert.IsInstanceOf<SdraPollingMessage>(o);

            var processMessage = _handlerContext.SentMessages[0];

            Assert.IsTrue(processMessage.Options.IsRoutingToThisEndpoint());
            Assert.AreEqual(TimeSpan.FromSeconds(5), processMessage.Options.GetDeliveryDelay());
        }
    }
}