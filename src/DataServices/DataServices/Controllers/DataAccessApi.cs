using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using DataServices.Adapters;
using DataServices.Attributes;
using DataServices.Connected_Services.SDRADataAccessWebService;
using DataServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Context;
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
        public async Task<IActionResult> DeleteDocumentRequestJobFromQueue([FromRoute][Required]string callerCode, [FromRoute][Required]int? sdocId, [FromQuery][Required]string writeableFolderName)
        {
            LogContext.PushProperty("ApiResource", nameof(DeleteDocumentRequestJobFromQueue));
            LogContext.PushProperty("CallerCode", callerCode);
            LogContext.PushProperty("SdocId", sdocId);
            LogContext.PushProperty("WriteableFolderName", writeableFolderName);

            _logger.LogInformation("{ApiResource} entered with: CallerCode: {CallerCode}; SdocId: {SdocId}; WriteableFolderName: {WriteableFolderName};");

            if (!sdocId.HasValue || sdocId <= 0)
                throw new ArgumentException("Error retrieving Backward linked document due to invalid parameter", nameof(sdocId));

            ReturnCode returnCode;
            try
            {
                LogContext.PushProperty("WebServiceRequestResource", nameof(_dataAccessWebServiceSoapClientAdapter.SoapClient.ClearDocumentRequestJobFromQueueAsync));
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource}");

                var task = await _dataAccessWebServiceSoapClientAdapter.SoapClient.ClearDocumentRequestJobFromQueueAsync(callerCode, sdocId.Value, writeableFolderName);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");

                returnCode = new ReturnCode
                {
                    Code = task.ErrorCode,
                    Message = task.Message
                };
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error clearing document from queue");
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
        public async Task<IActionResult> GetBackwardDocumentLinks([FromRoute][Required]int? sdocId)
        {
            LogContext.PushProperty("ApiResource", nameof(GetBackwardDocumentLinks));
            LogContext.PushProperty("SdocId", sdocId);

            _logger.LogInformation("{ApiResource} entered with: SdocId: {SdocId};");

            if (!sdocId.HasValue || sdocId <= 0)
                throw new ArgumentException("Error retrieving Backward linked document due to invalid parameter", nameof(sdocId));

            Link[] result;

            try
            {
                LogContext.PushProperty("WebServiceRequestResource", nameof(_dataAccessWebServiceSoapClientAdapter.SoapClient.GetBackwardDocumentLinksAsync));
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource}");

                result = await _dataAccessWebServiceSoapClientAdapter.SoapClient.GetBackwardDocumentLinksAsync(sdocId.Value);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error retrieving Backward linked document metadata");
                return StatusCode(500, e.Message);
            }

            var documents = result.Select<Link, LinkedDocument>(link => new LinkedDocument()
            {
                DocId1 = link.DocId1,
                DocId2 = link.DocId2,
                LinkType = link.LinkType
            });

            var linkedDocuments = new LinkedDocuments();

            linkedDocuments.AddRange(documents);

            return new ObjectResult(linkedDocuments);
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
        [Route("/DataServices/v1/SourceDocument/DataAccess/DocumentAssessmentData/{sdocId}")]
        [ValidateModelState]
        [SwaggerOperation("GetDocumentAssessmentData")]
        [SwaggerResponse(statusCode: 200, type: typeof(DocumentAssessmentData), description: "A document assessment data object")]
        [SwaggerResponse(statusCode: 400, type: typeof(DefaultErrorResponse), description: "Bad request.")]
        [SwaggerResponse(statusCode: 401, type: typeof(DefaultErrorResponse), description: "Unauthorised.")]
        [SwaggerResponse(statusCode: 403, type: typeof(DefaultErrorResponse), description: "Forbidden.")]
        [SwaggerResponse(statusCode: 404, type: typeof(DefaultErrorResponse), description: "Not found.")]
        [SwaggerResponse(statusCode: 406, type: typeof(DefaultErrorResponse), description: "Not acceptable.")]
        [SwaggerResponse(statusCode: 500, type: typeof(DefaultErrorResponse), description: "Internal Server Error.")]
        public async Task<IActionResult> GetDocumentAssessmentData([FromRoute][Required]int? sdocId)
        {
            LogContext.PushProperty("ApiResource", nameof(GetDocumentAssessmentData));
            LogContext.PushProperty("SdocId", sdocId);

            _logger.LogInformation("{ApiResource} entered with: SdocId: {SdocId};");

            DocumentAssessmentData data;

            try
            {
                _logger.LogInformation("{ApiResource} retrieving AssessmentData");

                var retrievedData = _dbContext.AssessmentData.Where(x => x.SdocId == sdocId.Value).ToList();
                data = retrievedData.Single();

                _logger.LogInformation("{ApiResource} finished retrieving AssessmentData successfully");
            }
            catch (DbException e)
            {
                _logger.LogError(e,
                    "{ApiResource} Error retrieving document assessment data for SdocId: {SdocId} from the database.");
                return StatusCode(500, e.Message);
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(e,
                    "{ApiResource} No document assessment data retrieved for SdocId: {SdocId}.");
                return StatusCode(500, e.Message);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e,
                    "{ApiResource} No document assessment data retrieved for SdocId: {SdocId}.");
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
        public async Task<IActionResult> GetDocumentForViewing([FromRoute][Required]string callerCode, [FromRoute][Required]int? sdocId, [FromRoute][Required]bool? imageAsGeoTIFF, [FromQuery][Required]string writeableFolderName)
        {
            LogContext.PushProperty("ApiResource", nameof(GetDocumentForViewing));
            LogContext.PushProperty("CallerCode", callerCode);
            LogContext.PushProperty("SdocId", sdocId);
            LogContext.PushProperty("ImageAsGeoTIFF", imageAsGeoTIFF);
            LogContext.PushProperty("WriteableFolderName", writeableFolderName);

            _logger.LogInformation("{ApiResource} entered with: CallerCode: {CallerCode}; SdocId: {SdocId}; ImageAsGeoTIFF: {ImageAsGeoTIFF}; WriteableFolderName: {WriteableFolderName};");

            ReturnCode returnCode;
            try
            {
                LogContext.PushProperty("WebServiceRequestResource", nameof(_dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentForViewingAsync));
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource}");

                var task = await _dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentForViewingAsync(callerCode, sdocId.Value, writeableFolderName, imageAsGeoTIFF.Value);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");

                returnCode = new ReturnCode
                {
                    Code = task.ErrorCode,
                    Message = task.Message
                };
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error initiating document retrieval");
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
        public async Task<IActionResult> GetDocumentRequestQueueStatus([FromRoute][Required]string callerCode)
        {
            LogContext.PushProperty("ApiResource", nameof(GetDocumentRequestQueueStatus));
            LogContext.PushProperty("CallerCode", callerCode);

            _logger.LogInformation("{ApiResource} entered with: CallerCode: {CallerCode};");

            ProcessingStatus[] docStatus;
            try
            {
                LogContext.PushProperty("WebServiceRequestResource", nameof(_dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentRequestQueueStatusAsync));
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource}");

                docStatus = await _dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentRequestQueueStatusAsync(callerCode);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error retrieving document status");
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
        public async Task<IActionResult> GetForwardDocumentLinks([FromRoute][Required]int? sdocId)
        {
            LogContext.PushProperty("ApiResource", nameof(GetForwardDocumentLinks));
            LogContext.PushProperty("SdocId", sdocId);

            _logger.LogInformation("{ApiResource} entered with: SdocId: {SdocId};");

            if (!sdocId.HasValue || sdocId <= 0)
                throw new ArgumentException("Error retrieving Forward linked document due to invalid parameter", nameof(sdocId));

            Link[] result;

            try
            {
                LogContext.PushProperty("WebServiceRequestResource", nameof(_dataAccessWebServiceSoapClientAdapter.SoapClient.GetForwardDocumentLinksAsync));
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource}");

                result = await _dataAccessWebServiceSoapClientAdapter.SoapClient.GetForwardDocumentLinksAsync(sdocId.Value);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error retrieving Forward linked document metadata");
                return StatusCode(500, e.Message);
            }

            var documents = result.Select<Link, LinkedDocument>(link => new LinkedDocument()
            {
                DocId1 = link.DocId1,
                DocId2 = link.DocId2,
                LinkType = link.LinkType
            });

            var linkedDocuments = new LinkedDocuments();

            linkedDocuments.AddRange(documents);

            return new ObjectResult(linkedDocuments);
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
        public async Task<IActionResult> GetSepDocumentLinks([FromRoute][Required]int? sdocId)
        {
            LogContext.PushProperty("ApiResource", nameof(GetSepDocumentLinks));
            LogContext.PushProperty("SdocId", sdocId);

            _logger.LogInformation("{ApiResource} entered with: SdocId: {SdocId};");
            
            if (!sdocId.HasValue || sdocId <= 0)
                throw new ArgumentException("Error retrieving SEP linked document due to invalid parameter", nameof(sdocId));

            Document[] result;

            try
            {
                LogContext.PushProperty("WebServiceRequestResource", nameof(_dataAccessWebServiceSoapClientAdapter.SoapClient.GetSEPDocumentLinksAsync));
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource}");

                result = await _dataAccessWebServiceSoapClientAdapter.SoapClient.GetSEPDocumentLinksAsync(sdocId.Value);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");
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
        public async Task<IActionResult> GetDocumentsFromList([FromQuery(Name = "sdocIds")][Required]int[] sdocIds)
        {
            LogContext.PushProperty("ApiResource", nameof(GetDocumentsFromList));
            LogContext.PushProperty("SdocIds", sdocIds);

            _logger.LogInformation("{ApiResource} entered with: SdocIds: {SdocIds};");

            if (sdocIds == null || sdocIds.Length == 0)
                throw new ArgumentException("Error retrieving linked document metadata due to invalid parameter", nameof(sdocIds));

            Document[] result;

            try
            {
                LogContext.PushProperty("WebServiceRequestResource", nameof(_dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentsFromListAsync));
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource}");

                result = await _dataAccessWebServiceSoapClientAdapter.SoapClient.GetDocumentsFromListAsync(sdocIds);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error retrieving linked document metadata");
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
        public async Task<IActionResult> ClearDocumentRequestQueue([FromRoute][Required]string callerCode)
        {
            LogContext.PushProperty("ApiResource", nameof(ClearDocumentRequestQueue));
            LogContext.PushProperty("CallerCode", callerCode);

            _logger.LogInformation("{ApiResource} entered with: CallerCode: {CallerCode};");

            CallOutcome outcome;
            try
            {
                LogContext.PushProperty("WebServiceRequestResource", nameof(_dataAccessWebServiceSoapClientAdapter.SoapClient.ClearDocumentRequestQueueAsync));
                _logger.LogInformation("{ApiResource} requesting Web Service {WebServiceRequestResource}");

                outcome = await _dataAccessWebServiceSoapClientAdapter.SoapClient.ClearDocumentRequestQueueAsync(callerCode);

                _logger.LogInformation("{ApiResource} finished requesting Web Service {WebServiceRequestResource} successfully");
            }
            catch (AggregateException e) when (e.InnerException is System.ServiceModel.EndpointNotFoundException)
            {
                _logger.LogError(e, "{ApiResource} Endpoint not found");
                return StatusCode(500, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{ApiResource} Error clearing document queue");
                return StatusCode(500, e.Message);
            }
            return new ObjectResult(outcome);
        }
    }
}
