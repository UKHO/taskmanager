using System;
using System.Threading.Tasks;
using Common.Messages.Commands;
using Common.Messages.Events;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;

namespace EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DefaultController : ControllerBase
    {
        private readonly IMessageSession _messageSession;

        public DefaultController(IMessageSession messageSession)
        {
            _messageSession = messageSession;
        }

        [HttpGet]
        [Route("getit")]
        public async Task<string> GetIt()
        {
            var message = new GregTestEvent()
            {
                CorrelationId = Guid.NewGuid(),
                Gregio = "blah"
            };
            var publishOptions = new PublishOptions();
            await _messageSession.Publish(message, publishOptions).ConfigureAwait(false);
            //var sendOptions = new SendOptions();
            //sendOptions.SetDestination("UKHO.TaskManager.SourceDocumentCoordinator");
            //await _messageSession.Send(message, sendOptions)
            //    .ConfigureAwait(false);
            return "Message sent to endpoint";
        }
    }
}