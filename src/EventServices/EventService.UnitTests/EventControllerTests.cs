using System;
using System.Threading.Tasks;
using Common.Messages.Enums;
using Common.Messages.Events;
using NServiceBus.Testing;
using NUnit.Framework;

namespace EventService.UnitTests
{
    public class EventControllerTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test_publishing_requested_event_does_publish_event()
        {
            var testableSession = new TestableMessageSession();

            var eventController = new Controllers.EventController(testableSession);

            var theEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = Guid.NewGuid(),
                ProcessId = 123,
                SourceDocumentId = 12345,
                DocumentType = SourceDocumentType.Primary,
                GeoReferenced = false
            };

            await eventController.PostEvent(System.Text.Json.JsonSerializer.Serialize(theEvent, typeof(InitiateSourceDocumentRetrievalEvent), null), "InitiateSourceDocumentRetrievalEvent");

            Assert.AreEqual(1, testableSession.PublishedMessages.Length);
            Assert.IsInstanceOf<InitiateSourceDocumentRetrievalEvent>(testableSession.PublishedMessages[0].Message);
        }
    }
}