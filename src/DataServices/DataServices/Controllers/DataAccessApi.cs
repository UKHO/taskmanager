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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using DataServices.Adapters;
using DataServices.Attributes;
using DataServices.Connected_Services.SDRADataAccessWebService;
using DataServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace DataServices.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiController]
    public class DataAccessApiController : ControllerBase
    {
        private readonly SdraDbContext _dbContext;
        private readonly ILogger<DataAccessApiController> _logger;
        private readonly IDataAccessWebServiceSoapClientAdapter _dataAccessWebServiceSoapClientAdapter;

        public DataAccessApiController(SdraDbContext dbContext, ILogger<DataAccessApiController> logger,
            IDataAccessWebServiceSoapClientAdapter dataAccessWebServiceSoapClientAdapter)
        {
            _dbContext = dbContext;
            _logger = logger;
            _dataAccessWebServiceSoapClientAdapter = dataAccessWebServiceSoapClientAdapter;
        }

        /// <summary>
        /// Clear the document request job from the queue
        /// </summary>
        /// <param name="callerCode">System that is calling the API  Example: HDB </param>
        /// <param name="sdocId">Unique identifier for an SDRA Source Document   Example:  </param>
        /// <param name="writeableFolderName">The path for the file output Example: tbc </param>
        /// <response code="200">An code and message</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpDelete]
        [Route("/DataServices/v1/SourceDocument/DataAccess/ClearDocumentRequestJobFromQueue/{callerCode}/{sdocId}")]
        [ValidateModelState]
        [SwaggerOperation("DeleteDocumentRequestJobFromQueue")]
        [SwaggerResponse(statusCode: 200, type: typeof(ReturnCode), description: "An code and message")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult DeleteDocumentRequestJobFromQueue([FromRoute][Required]string callerCode, [FromRoute][Required]int? sdocId, [FromQuery][Required]string writeableFolderName)
        {
            var task = _dataAccessWebServiceSoapClientAdapter.SoapClient.ClearDocumentRequestJobFromQueueAsync(callerCode,
                sdocId.Value,
                writeableFolderName);

            ReturnCode returnCode = null;
            try
            {
                returnCode = new ReturnCode
                {
                    Code = task.Result.ErrorCode,
                    Message = task.Result.Message
                };
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error clearing document from queue");
                return StatusCode(500, e.Message);
            }
            return new ObjectResult(returnCode);
        }

        /// <summary>
        /// Get documents linked to source document assessment
        /// </summary>
        /// <param name="sdocId">Unique identifier for an SDRA Source Document   Example:  </param>
        /// <response code="200">An array of documents linked to the assessment</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/DataServices/v1/SourceDocument/DataAccess/BackwardDocumentLinks/{sdocId}")]
        [ValidateModelState]
        [SwaggerOperation("GetBackwardDocumentLinks")]
        [SwaggerResponse(statusCode: 200, type: typeof(LinkedDocument), description: "An array of documents linked to the assessment")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetBackwardDocumentLinks([FromRoute][Required]int? sdocId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(LinkedDocument));




            string exampleJson = null;
            exampleJson = "{\n  \"docId1\" : 0,\n  \"docId2\" : 6,\n  \"linkType\" : \"PARENTCHILD, CHARTPANELAFFECTED, CROSSREFERENCE\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<LinkedDocument>(exampleJson)
            : default(LinkedDocument);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Get the data for the document assessment
        /// </summary>
        /// <param name="callerCode">System that is calling the API  Example: HDB </param>
        /// <param name="sdocId">Unique identifier for an SDRA Source Document   Example:  </param>
        /// <response code="200">A document assessment data object</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/DataServices/v1/SourceDocument/DataAccess/DocumentAssessmentData/{callerCode}/{sdocId}")]
        [ValidateModelState]
        [SwaggerOperation("GetDocumentAssessmentData")]
        [SwaggerResponse(statusCode: 200, type: typeof(DocumentAssessmentData), description: "A document assessment data object")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetDocumentAssessmentData([FromRoute][Required]string callerCode, [FromRoute][Required]int? sdocId)
        {
            DocumentAssessmentData data = null;

            try
            {
                var retrievedData = _dbContext.AssessmentData.Where(x => x.SdocId == sdocId).ToList();
                data = retrievedData.Single();
            }
            catch (DbException e)
            {
                _logger.LogError(e,
                    $"Error retrieving document assessment data for sdocId: {sdocId} from the database.");
                return StatusCode(500, e.Message);
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(e,
                    $"No document assessment data retrieved for sdocId: {sdocId}.");
                return StatusCode(500, e.Message);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e,
                    $"No document assessment data retrieved for sdocId: {sdocId}.");
                return StatusCode(500, e.Message);
            }

            return new ObjectResult(data);
        }

        /// <summary>
        /// Get the Document URL for the Id specified.
        /// </summary>
        /// <param name="callerCode">System that is calling the API  Example: HDB </param>
        /// <param name="sdocId">Unique identifier for an SDRA Source Document   Example:  </param>
        /// <param name="writeableFolderName">The path for the file output Example: tbc </param>
        /// <param name="imageAsGeoTIFF">True if conversion to GeoTiff should be attempted. Example: true, false </param>
        /// <response code="200">A code and description of the document request status</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/DataServices/v1/SourceDocument/DataAccess/DocumentForViewing/{callerCode}/{sdocId}/{imageAsGeoTIFF}")]
        [ValidateModelState]
        [SwaggerOperation("GetDocumentForViewing")]
        [SwaggerResponse(statusCode: 200, type: typeof(ReturnCode), description: "A code and description of the document request status")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetDocumentForViewing([FromRoute][Required]string callerCode, [FromRoute][Required]int? sdocId, [FromRoute][Required]bool? imageAsGeoTIFF, [FromQuery][Required]string writeableFolderName)
        {
            var task = _dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentForViewingAsync(callerCode, sdocId.Value,
                writeableFolderName, imageAsGeoTIFF.Value);

            ReturnCode returnCode = null;
            try
            {
                returnCode = new ReturnCode
                {
                    Code = task.Result.ErrorCode,
                    Message = task.Result.Message
                };
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error initiating document retrieval");
                return StatusCode(500, e.Message);
            }
            return new ObjectResult(returnCode);
        }

        /// <summary>
        /// Get the status of all queued documents
        /// </summary>
        /// <param name="callerCode">System that is calling the API  Example: HDB </param>
        /// <response code="200">An array of queued document objects</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/DataServices/v1/SourceDocument/DataAccess/DocumentRequestQueueStatus/{callerCode}")]
        [ValidateModelState]
        [SwaggerOperation("GetDocumentRequestQueueStatus")]
        [SwaggerResponse(statusCode: 200, type: typeof(QueuedDocumentObjects), description: "An array of queued document objects")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetDocumentRequestQueueStatus([FromRoute][Required]string callerCode)
        {
            var task = _dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentRequestQueueStatusAsync(callerCode);

            ProcessingStatus[] docStatus = null;
            try
            {
                docStatus = task.Result;
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving document status");
                return StatusCode(500, e.Message);
            }

            var queuedDocumentObjects = new QueuedDocumentObjects();
            var documents = docStatus.Select<ProcessingStatus, QueuedDocumentObject>(
                document => new QueuedDocumentObject
                {
                    Code = document.ErrorCode,
                    Message = document.Message,
                    SodcId = document.SdocId,
                    StatusTime = document.StatusTime
                });

            queuedDocumentObjects.AddRange(documents);

            return new ObjectResult(queuedDocumentObjects);

            //string exampleJson = null;
            //exampleJson = "[ {\n  \"code\" : 6,\n  \"statusTime\" : \"2000-01-23T04:56:07.000+00:00\",\n  \"message\" : \"The requested document has been added to the queue., The document is not of a type that is permitted to be exported from SDRA.,\\\\server\\\\SDRA\\1955393\\\\filename.pdf\",\n  \"sodcId\" : 0\n}, {\n  \"code\" : 6,\n  \"statusTime\" : \"2000-01-23T04:56:07.000+00:00\",\n  \"message\" : \"The requested document has been added to the queue., The document is not of a type that is permitted to be exported from SDRA.,\\\\servername\\\\SDRA\\1955393\\\\filename.pdf\",\n  \"sodcId\" : 0\n} ]";

            //var example = exampleJson != null
            //? JsonConvert.DeserializeObject<QueuedDocumentObjects>(exampleJson)
            //: default(QueuedDocumentObjects);            //TODO: Change the data returned
            //return new ObjectResult(example);
        }

        /// <summary>
        /// Get documents linked to source document assessment
        /// </summary>
        /// <param name="sdocId">Unique identifier for an SDRA Source Document   Example:  </param>
        /// <response code="200">AAn array of documents linked to the assessment</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/DataServices/v1/SourceDocument/DataAccess/ForwardDocumentLinks/{sdocId}")]
        [ValidateModelState]
        [SwaggerOperation("GetForwardDocumentLinks")]
        [SwaggerResponse(statusCode: 200, type: typeof(LinkedDocument), description: "AAn array of documents linked to the assessment")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetForwardDocumentLinks([FromRoute][Required]int? sdocId)
        {
            //TODO: Uncomment the next line to return response 200 or use other options such as return this.NotFound(), return this.BadRequest(..), ...
            // return StatusCode(200, default(LinkedDocument));

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
            string exampleJson = null;
            exampleJson = "{\n  \"docId1\" : 0,\n  \"docId2\" : 6,\n  \"linkType\" : \"PARENTCHILD, CHARTPANELAFFECTED, CROSSREFERENCE\"\n}";

            var example = exampleJson != null
            ? JsonConvert.DeserializeObject<LinkedDocument>(exampleJson)
            : default(LinkedDocument);            //TODO: Change the data returned
            return new ObjectResult(example);
        }

        /// <summary>
        /// Get list of document objects linked to source document assessment
        /// </summary>
        /// <param name="sdocId">Unique identifier for an SDRA Source Document   Example:  </param>
        /// <response code="200">An array of documents objects linked to the assessment</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/DataServices/v1/SourceDocument/DataAccess/SEPDocumentLinks/{sdocId}")]
        [ValidateModelState]
        [SwaggerOperation("GetSEPDocumentLinks")]
        [SwaggerResponse(statusCode: 200, type: typeof(DocumentObjects), description: "An array of documents objects linked to the assessment")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetSepDocumentLinks([FromRoute][Required]int? sdocId)
        {
            if (!sdocId.HasValue || sdocId <= 0)
                throw new ArgumentException("Error retrieving SEP linked document due to invalid parameter", nameof(sdocId));

            var task = _dataAccessWebServiceSoapClientAdapter.SoapClient.GetSEPDocumentLinksAsync(sdocId.Value);

            Document[] result;

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
                _logger.LogError(e, "Error retrieving SEP linked document metadata");
                return StatusCode(500, e.Message);
            }

            var documents = result.Select<Document, DocumentObject>(document => new DocumentObject()
            {
                Id = document.Id,
                Name = document.Name,
                SourceName = document.SourceName
            });

            var documentObjects = new DocumentObjects();

            documentObjects.AddRange(documents);

            return new ObjectResult(documentObjects);
        }

        /// <summary>
        /// Returns linked document metadata for the given SdocIds
        /// </summary>
        /// <param name="sdocIds">SdocIds to retrieve document metadata for</param>
        /// <response code="200">A code and message</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/DataServices/v1/SourceDocument/DataAccess/DocumentsFromList")]
        [ValidateModelState]
        [SwaggerOperation("GetDocumentsFromList")]
        [SwaggerResponse(statusCode: 200, type: typeof(ReturnCode), description: "A code and message")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult GetDocumentsFromList([FromQuery(Name = "sdocIds")][Required]int[] sdocIds)
        {
            if (sdocIds == null || sdocIds.Length == 0)
                throw  new ArgumentException("Error retrieving linked document metadata due to invalid parameter", nameof(sdocIds));

            var task = _dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentsFromListAsync(sdocIds);

            Document[] result;

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
                _logger.LogError(e, "Error retrieving linked document metadata");
                return StatusCode(500, e.Message);
            }

            var documents = result.Select<Document, DocumentObject>(document => new DocumentObject()
            {
                Id = document.Id,
                Name = document.Name,
                SourceName = document.SourceName
            });

            var documentObjects = new DocumentObjects();

            documentObjects.AddRange(documents);

            return new ObjectResult(documentObjects);
        }

        /// <summary>
        /// Clear the document request queue
        /// </summary>
        /// <param name="callerCode">System that is calling the API  Example: HDB </param>
        /// <response code="200">An code and message</response>
        /// <response code="400">Bad request.</response>
        /// <response code="401">Unauthorised.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Not found.</response>
        /// <response code="406">Not acceptable.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpDelete]
        [Route("/DataServices/v1/SourceDocument/DataAccess/ClearDocumentRequestQueue/{callerCode}")]
        [ValidateModelState]
        [SwaggerOperation("DeleteDocumentQueue")]
        [SwaggerResponse(statusCode: 200, type: typeof(ReturnCode), description: "An code and message")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public virtual IActionResult ClearDocumentRequestQueue([FromRoute][Required]string callerCode)
        {
            var task = _dataAccessWebServiceSoapClientAdapter.SoapClient.ClearDocumentRequestQueueAsync(callerCode);

            CallOutcome outcome = null;
            try
            {
                outcome = new CallOutcome
                {
                    ErrorCode = task.Result.ErrorCode,
                    Message = task.Result.Message
                };
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error clearing document queue");
                return StatusCode(500, e.Message);
            }
            return new ObjectResult(outcome);
        }
    }
}
