using System;
using System.Threading.Tasks;
using DataServices.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus.Testing;
using NUnit.Framework;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.Handlers;
using SourceDocumentCoordinator.HttpClients;
using SourceDocumentCoordinator.Messages;

namespace SourceDocumentCoordinator.UnitTests
{
    public class ClearDocumentRequestFromQueueCommandHandlerTests
    {
        private IDataServiceApiClient _fakeDataServiceApiClient;
        private TestableMessageHandlerContext _handlerContext;
        private ClearDocumentRequestFromQueueCommandHandler _handler;
        private ILogger<ClearDocumentRequestFromQueueCommandHandler> _fakeLogger;


        [SetUp]
        public void Setup()
        {
            _fakeDataServiceApiClient = A.Fake<IDataServiceApiClient>();
            _fakeLogger = A.Fake<ILogger<ClearDocumentRequestFromQueueCommandHandler>>();

            var generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();
            generalConfig.Value.CallerCode = "HBD";


            _handlerContext = new TestableMessageHandlerContext();
            _handler = new ClearDocumentRequestFromQueueCommandHandler(_fakeDataServiceApiClient, generalConfig, _fakeLogger);
        }

        [Test]
        public async Task Test_When_Document_Cleared_from_Queue_Then_Success_Is_Logged()
        {
            //Given
            var message = new ClearDocumentRequestFromQueueCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999
            };

            // When
            A.CallTo(() =>
                    _fakeDataServiceApiClient.DeleteDocumentRequestJobFromQueue("HBD", 1999999, A<string>.Ignored))
                .Returns(new ReturnCode());

            //When
            await _handler.Handle(message, _handlerContext).ConfigureAwait(false);

            //Assert
            Assert.DoesNotThrowAsync(() => _handler.Handle(message, _handlerContext));
        }


        [Test]
        public void Test_When_Clearing_Document_from_Queue_Throws_Exception_Then_handler_throws_exception()
        {
            //Given
            var message = new ClearDocumentRequestFromQueueCommand()
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 5678,
                SourceDocumentId = 1999999
            };

            // When
            A.CallTo(() =>
                    _fakeDataServiceApiClient.DeleteDocumentRequestJobFromQueue("HBD", 1999999, A<string>.Ignored))
                .Throws(new ApplicationException());

            //Assert
             Assert.ThrowsAsync<ApplicationException>(() => _handler.Handle(message, _handlerContext));

        }
    }
}
