using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Helpers;
using DataServices.Adapters;
using DataServices.Attributes;
using DataServices.Connected_Services.SDRAAssessmentWebService;
using DataServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Context;
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

        readonly ILogger<AssessmentApiController> _logger;

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
        public async Task<IActionResult> ListDocumentsForAssessment([FromRoute][Required]string callerCode)
        {
            LogContext.PushProperty("ApiResource", nameof(ListDocumentsForAssessment));
            LogContext.PushProperty("CallerCode", callerCode);
            
            _logger.LogInformation("{ApiResource} entered with: CallerCode: {CallerCode};");
            GetDocumentsForAssessmentResponse result = null;
            try
            {
                var getDocumentsForAssessmentRequest = new GetDocumentsForAssessmentRequest(new GetDocumentsForAssessmentRequestBody(callerCode));

                LogContext.PushProperty("WebServiceRequestResource", nameof(_assessmentWebServiceSoapClientAdapter.SoapClient.GetDocumentsForAssessmentAsync));
                LogContext.PushProperty("WebServiceRequestBody", getDocumentsForAssessmentRequest.Body.ToJSONSerializedString());
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource} with: WebServiceRequestBody: {WebServiceRequestBody};");

                result = await _assessmentWebServiceSoapClientAdapter.SoapClient.GetDocumentsForAssessmentAsync(
                    getDocumentsForAssessmentRequest);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error retrieving documents for assessment");
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
        [Route("/DataServices/v1/SourceDocument/Assessment/AssessmentCompleted/{callerCode}/{sdocId}")]
        [ValidateModelState]
        [SwaggerOperation("PutAssessmentAsCompleted")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public async Task<IActionResult> PutAssessmentAsCompleted([FromRoute] [Required] string callerCode,
            [FromRoute] [Required] int? sdocId, [FromQuery] [Required] string comment)
        {
            LogContext.PushProperty("ApiResource", nameof(PutAssessmentAsCompleted));
            LogContext.PushProperty("CallerCode", callerCode);
            LogContext.PushProperty("SdocId", sdocId);
            LogContext.PushProperty("Comment", comment);

            _logger.LogInformation("{ApiResource} entered with: CallerCode: {CallerCode}; SdocId: {SdocId}; Comment: {Comment};");

            if (!sdocId.HasValue || sdocId <= 0)
                throw new ArgumentException("Error marking assessment complete due to invalid parameter", nameof(sdocId));

            CallOutcome result;

            try
            {
                var notifyAssessmentCompletedRequest = new NotifyAssessmentCompletedRequest(new NotifyAssessmentCompletedRequestBody(callerCode, sdocId.Value, comment));

                LogContext.PushProperty("WebServiceRequestResource", nameof(_assessmentWebServiceSoapClientAdapter.SoapClient.NotifyAssessmentCompletedAsync));
                LogContext.PushProperty("WebServiceRequestBody", notifyAssessmentCompletedRequest.Body.ToJSONSerializedString());
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource} with: WebServiceRequestBody: {@WebServiceRequestBody};");

                var task = await _assessmentWebServiceSoapClientAdapter.SoapClient.NotifyAssessmentCompletedAsync(
                    notifyAssessmentCompletedRequest);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");

                result = task.Body.NotifyAssessmentCompletedResult;
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error marking assessment complete");
                return StatusCode(500, e.Message);
            }

            if (result.ErrorCode != 0)
            {
                LogContext.PushProperty("WebServiceResponseMessage", result.Message);
                _logger.LogError("{ApiResource} Error marking assessment complete, WebServiceResponseMessage: {WebServiceResponseMessage}");

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
        /// <param name="actionType">A action type from a list  Example: Imm Act - NM, Longer-term Action, No Action, No Impact </param>
        /// <param name="change">tbc  Example: tbc </param>
        /// <response code="200">SDRA notified that document has been assessment</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPut]
        [Route("/DataServices/v1/SourceDocument/Assessment/DocumentAssessed/{callerCode}/{transactionId}/{sdocId}/{actionType}")]
        [ValidateModelState]
        [SwaggerOperation("PutDocumentAsAssessed")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public async Task<IActionResult> PutDocumentAsAssessed(
                                                            [FromRoute][Required]string callerCode, 
                                                            [FromRoute][Required]string transactionId,
                                                            [FromRoute][Required]int? sdocId,
                                                            [FromRoute][Required]string actionType, 
                                                            [FromQuery][Required]string change)
        {

            LogContext.PushProperty("ApiResource", nameof(PutDocumentAsAssessed));
            LogContext.PushProperty("CallerCode", callerCode);
            LogContext.PushProperty("SdocId", sdocId??0);
            LogContext.PushProperty("TransactionId", transactionId);
            LogContext.PushProperty("ActionType", actionType);
            LogContext.PushProperty("Change", change);

            _logger.LogInformation("{ApiResource} entered with: CallerCode: {CallerCode}; " +
                                               "SdocId: {SdocId}; " +
                                               "TransactionId: {TransactionId}; " +
                                               "ActionType: {ActionType}; " +
                                               "Change: {Change}");

            if (!sdocId.HasValue || sdocId <= 0)
                throw new ArgumentException("Error marking assessment Assessed due to invalid parameter", nameof(sdocId));

            CallOutcome result;

            try
            {
                var notifyAssessmentCompletedRequest = new NotifyDocumentAssessedRequest(
                                                    new NotifyDocumentAssessedRequestBody(
                                                                                            callerCode, 
                                                                                            transactionId, 
                                                                                            sdocId.Value,
                                                                                            0, 
                                                                                            null,
                                                                                            actionType, 
                                                                                            change));

                LogContext.PushProperty("WebServiceRequestResource", nameof(_assessmentWebServiceSoapClientAdapter.SoapClient.NotifyDocumentAssessedAsync));
                LogContext.PushProperty("WebServiceRequestBody", notifyAssessmentCompletedRequest.Body.ToJSONSerializedString());
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource} with: WebServiceRequestBody: {@WebServiceRequestBody};");

                var task = await _assessmentWebServiceSoapClientAdapter.SoapClient.NotifyDocumentAssessedAsync(notifyAssessmentCompletedRequest);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");

                result = task.Body.NotifyDocumentAssessedResult;
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error marking assessment Assessed");
                return StatusCode(500, e.Message);
            }

            if (result.ErrorCode != 0)
            {
                LogContext.PushProperty("WebServiceResponseMessage", result.Message);
                _logger.LogError("{ApiResource} Error marking assessment Assessed, WebServiceResponseMessage: {WebServiceResponseMessage}");

                return StatusCode(500, $"Error marking assessment Assessed, Message: {result.Message}");
            }

            return new ObjectResult(HttpStatusCode.OK);
        }
    }
}
