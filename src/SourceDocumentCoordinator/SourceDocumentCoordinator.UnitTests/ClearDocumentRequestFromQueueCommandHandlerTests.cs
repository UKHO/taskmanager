using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NServiceBus.Testing;
using NUnit.Framework;
using SourceDocumentCoordinator.Config;
using SourceDocumentCoordinator.Handlers;
using SourceDocumentCoordinator.HttpClients;

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
            _fakeLogger = A.Dummy<ILogger<ClearDocumentRequestFromQueueCommandHandler>>();

            var generalConfig = A.Fake<IOptionsSnapshot<GeneralConfig>>();


            _handlerContext = new TestableMessageHandlerContext();
            _handler = new ClearDocumentRequestFromQueueCommandHandler(_fakeDataServiceApiClient, generalConfig, _fakeLogger);
        }


    }
}
