using System;
using System.Threading.Tasks;
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

            var theEvent = new GregTestEvent
            {
                CorrelationId = Guid.NewGuid(),
                Gregio = "Wow"
            };

            await eventController.PostEvent(System.Text.Json.JsonSerializer.Serialize(theEvent, typeof(GregTestEvent), null), "GregTestEvent");

            Assert.AreEqual(1, testableSession.PublishedMessages.Length);
            Assert.IsInstanceOf<GregTestEvent>(testableSession.PublishedMessages[0].Message);
        }
    }
}