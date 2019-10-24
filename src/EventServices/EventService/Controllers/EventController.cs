using System;
using System.ComponentModel.DataAnnotations;
using EventService.Attributes;
using EventService.Models;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;
using Swashbuckle.AspNetCore.Annotations;

namespace EventService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IMessageSession _messageSession;

        public EventController(IMessageSession messageSession)
        {
            _messageSession = messageSession;
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
        public virtual IActionResult PostEvent([FromBody]Object body, [FromRoute][Required]string eventName)
        {
            var publishOptions = new PublishOptions();
            //await _messageSession.Publish(message, publishOptions).ConfigureAwait(false);
            //var sendOptions = new SendOptions();
            //sendOptions.SetDestination("UKHO.TaskManager.SourceDocumentCoordinator");
            //await _messageSession.Send(message, sendOptions)
            //    .ConfigureAwait(false);
            return null; //"Message sent to endpoint";
        }
    }
}