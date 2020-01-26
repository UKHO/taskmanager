using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Common.Messages;
using EventService.Attributes;
using EventService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Serilog.Context;
using Swashbuckle.AspNetCore.Annotations;

namespace EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IMessageSession _messageSession;
        private readonly ILogger<EventController> _logger;

        public EventController(IMessageSession messageSession, ILogger<EventController> logger)
        {
            _messageSession = messageSession;
            _logger = logger;
        }

        /// <summary>
        /// Get specific event by name
        /// </summary>
        /// <param name="eventName">The name of the event Example: HDB </param>
        /// <response code="200">Returns the event requested</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/EventService/v1/Workflow/Event/{eventName}")]
        [ValidateModelState]
        [SwaggerOperation("GetEvent")]
        [SwaggerResponse(statusCode: 200, type: typeof(Event), description: "Returns the event requested")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetEvent([FromRoute][Required]string eventName)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(Event));

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 401 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(401, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 406 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(406, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(DefaultErrorResponse));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Get events
        /// </summary>
        /// <response code="200">An array of event objects</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/EventService/v1/Workflow/Event/")]
        [ValidateModelState]
        [SwaggerOperation("GetEvents")]
        [SwaggerResponse(statusCode: 200, type: typeof(EventObjects), description: "An array of event objects")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetEvents()
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(EventObjects));

            //TODO: Uncomment the next line to return response 400 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(400, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 401 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(401, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 403 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(403, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 404 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(404, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 406 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(406, default(DefaultErrorResponse));

            //TODO: Uncomment the next line to return response 500 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(500, default(DefaultErrorResponse));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new event.
        /// </summary>
        /// <param name="body">The body of the event to post</param>
        /// <param name="eventName">The name of the event Example: HDB</param>
        /// <response code="200">Event successfully posted</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost]
        [Route("/EventService/v1/Workflow/Event/{eventName}")]
        [ValidateModelState]
        [SwaggerOperation("PostEvent")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        [SwaggerResponse(statusCode: 503, type: typeof(DefaultErrorResponse), description: "Service Unavailable.")]
        public virtual async Task<IActionResult> PostEvent([FromBody]object body, [FromRoute][Required]string eventName)
        {
            LogContext.PushProperty("ApiResource", nameof(PostEvent));
            LogContext.PushProperty("EventName", eventName);
            LogContext.PushProperty("EventBody", body);

            _logger.LogInformation("{ApiResource} entered with event {EventName}");
            // Use reflection to discover events, retrieve the correct event by name and 
            // deserialize it via the provided JSON body.
            Assembly assembly = null;
            Type eventType = null;
            object populatedEvent = null;

            try
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace(@"file:\", "");
                var assemblyPath = Path.Combine(path, "Common.Messages.dll");
                assembly = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Failed to load assembly Common.Messages");
                return StatusCode(500);
            }

            try
            {
                var events = assembly.GetTypes().ToList();
                eventType = events.First(x => x.Name == eventName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Failed to get event type {EventName}");
                return StatusCode(500, $"Failed to get event type {eventName}");
            }

            try
            {
                populatedEvent = System.Text.Json.JsonSerializer.Deserialize(body.ToString(), eventType, null);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Failed to deserialize event {EventName}");
                return StatusCode(500, $"Failed to deserialize event {eventName}");
            }

            try
            {
                var correlationId = ((ICorrelate)populatedEvent).CorrelationId;

                if (correlationId.Equals(Guid.Empty))
                {
                    throw new InvalidCastException();
                }

                LogContext.PushProperty("CorrelationId", correlationId);
            }
            catch (InvalidCastException e)
            {
                _logger.LogError(e, "{ApiResource} Could not get CorrelationId from {EventName}");
                return StatusCode(500, $"Failed to cast event using ICorrelate from {eventName}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Could not get CorrelationId from {EventName}");
                return StatusCode(500, $"Failed to interpret CorrelationId from {eventName}");
            }

            try
            {
                var publishOptions = new PublishOptions();

                _logger.LogInformation("{ApiResource} publishing event {EventName}");
                await _messageSession.Publish(populatedEvent, publishOptions).ConfigureAwait(false);
                _logger.LogInformation("{ApiResource} finished publishing event {EventName}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Failed to publish event {EventName}");
                return StatusCode(503, $"Failed to publish event {eventName}. Please try again later.");
            }

            return new ObjectResult(HttpStatusCode.OK);
        }
    }
}