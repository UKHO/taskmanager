/*
 * SDRA API
 *
 * This API is for SDRA  It provides Source Document Assessment Data and Data Access 
 *
 * OpenAPI spec version: 1.0.0
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DataServices.Adapters;
using DataServices.Attributes;
using DataServices.Connected_Services.SDRAAssessmentWebService;
using DataServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Document = DataServices.Connected_Services.SDRAAssessmentWebService.Document;

namespace DataServices.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    public class AssessmentApiController : ControllerBase
    {
        private readonly IAssessmentWebServiceSoapClientAdapter _assessmentWebServiceSoapClientAdapter;

        private readonly ILogger _logger;

        public AssessmentApiController(
            IAssessmentWebServiceSoapClientAdapter assessmentWebServiceSoapClientAdapter,
            ILogger<AssessmentApiController> logger)
        {
            _assessmentWebServiceSoapClientAdapter = assessmentWebServiceSoapClientAdapter;
            _logger = logger;
        }

        /// <summary>
        /// Get a list of open source document objects
        /// </summary>
        /// <param name="callerCode">System that is calling the API  Example: HDB </param>
        /// <response code="200">An array of document objects</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/DataServices/v1/SourceDocument/Assessment/DocumentsForAssessment/{callerCode}")]
        [ValidateModelState]
        [SwaggerOperation("ListDocumentsForAssessment")]
        [SwaggerResponse(statusCode: 200, type: typeof(DocumentObjects), description: "An array of document objects")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public IActionResult ListDocumentsForAssessment([FromRoute][Required]string callerCode)
        {
            var task = _assessmentWebServiceSoapClientAdapter.SoapClient.GetDocumentsForAssessmentAsync(
                new GetDocumentsForAssessmentRequest(new GetDocumentsForAssessmentRequestBody(callerCode)));

            GetDocumentsForAssessmentResponse result = null;
            try
            {
                result = task.Result;
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving documents for assessment");
                return StatusCode(500, e.Message);
            }

            var documentObjects = new DocumentObjects();
            var documents = result.Body.GetDocumentsForAssessmentResult.Select<Document, DocumentObject>(
                document => new DocumentObject
                {
                    Id = document.Id,
                    Name = document.Name,
                    SourceName = document.SourceName
                });

            documentObjects.AddRange(documents);

            return new ObjectResult(documentObjects);
        }

        /// <summary>
        /// Notify SDRA that assessment job has been completed.
        /// </summary>
        /// <param name="callerCode">System that is calling the API  Example: HDB </param>
        /// <param name="sdocId">Unique identifier for an SDRA Source Document   Example:  </param>
        /// <param name="comment">tbc </param>
        /// <response code="200">SDRA notified that assessment has been completed</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPut]
        [Route("/DataServices/v1/SourceDocument/Assessment/AssessmentCompleted/{callerCode}/{sdocId}/{comment}")]
        [ValidateModelState]
        [SwaggerOperation("PutAssessmentCompleted")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public IActionResult PutAssessmentCompleted([FromRoute] [Required] string callerCode,
            [FromRoute] [Required] int? sdocId, [FromRoute] [Required] string comment)
        {
            if (!sdocId.HasValue || sdocId <= 0)
                throw new ArgumentException("Error marking assessment complete due to invalid parameter", nameof(sdocId));

            var task = _assessmentWebServiceSoapClientAdapter.SoapClient.NotifyAssessmentCompletedAsync(
                new NotifyAssessmentCompletedRequest(new NotifyAssessmentCompletedRequestBody(callerCode, sdocId.Value, comment)));

            CallOutcome result;

            try
            {
                result = task.Result.Body.NotifyAssessmentCompletedResult;
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error marking assessment complete");
                return StatusCode(500, e.Message);
            }

            if (result.ErrorCode != 0)
            {
                _logger.LogError($"Error marking assessment complete, Message: {result.Message}");

                return StatusCode(500, $"Error marking assessment complete, Message: {result.Message}");
            }

            return new ObjectResult(HttpStatusCode.OK);
        }

        /// <summary>
        /// Create assessment record for specified document.
        /// </summary>
        /// <param name="callerCode">System that is calling the API  Example: HDB </param>
        /// <param name="transactionId">Cross reference from SDRA to  external system providing the assessment record   Example: tbc </param>
        /// <param name="sdocId">Unique identifier for an SDRA Source Document   Example:  </param>
        /// <param name="action">A action type from a list  Example: Imm Act - NM, Longer-term Action, No Action, No Impact </param>
        /// <param name="change">tbc  Example: tbc </param>
        /// <response code="200">SDRA notified that document has been assessment</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPut]
        [Route("/DataServices/v1/SourceDocument/Assessment/DocumentAssessed/{callerCode}/{transactionId}/{sdocId}/{actionType}/{change}")]
        [ValidateModelState]
        [SwaggerOperation("PutDocumentAssessed")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public IActionResult PutDocumentAssessed([FromRoute][Required]string callerCode, [FromRoute][Required]string transactionId, [FromRoute][Required]int? sdocId, [FromRoute][Required]string actionType, [FromRoute][Required]string change)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200);

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
    }
}
