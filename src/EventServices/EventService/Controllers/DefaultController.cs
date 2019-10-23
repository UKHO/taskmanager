using System.Threading.Tasks;
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
            var message = new GregTestEvent();
            await _messageSession.Send(message)
                .ConfigureAwait(false);
            return "Message sent to endpoint";
        }
    }
}