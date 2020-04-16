using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Helpers;
using Common.Messages.Enums;
using Common.Messages.Events;
using DataServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Portal.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;
using LinkedDocument = WorkflowDatabase.EF.Models.LinkedDocument;

namespace Portal.Pages.DbAssessment
{
    public class _SourceDocumentDetailsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly IDataServiceApiClient _dataServiceApiClient;
        private readonly IDocumentStatusFactory _documentStatusFactory;
        private readonly ILogger<_SourceDocumentDetailsModel> _logger;

        [BindProperty(SupportsGet = true)] public int ProcessId { get; set; }
        public AssessmentData Assessment { get; set; }
        public IEnumerable<LinkedDocument> LinkedDocuments { get; set; }
        public IEnumerable<LinkedDocument> AttachedLinkedDocuments { get; set; }
        public PrimaryDocumentStatus PrimaryDocumentStatus { get; set; }
        public IEnumerable<DatabaseDocumentStatus> DatabaseDocuments { get; set; }

        public _SourceDocumentDetailsModel(WorkflowDbContext dbContext,
            IOptions<UriConfig> uriConfig, IEventServiceApiClient eventServiceApiClient,
            IDataServiceApiClient dataServiceApiClient, IDocumentStatusFactory documentStatusFactory,
            ILogger<_SourceDocumentDetailsModel> logger)
        {
            _dbContext = dbContext;
            _uriConfig = uriConfig;
            _eventServiceApiClient = eventServiceApiClient;
            _dataServiceApiClient = dataServiceApiClient;
            _documentStatusFactory = documentStatusFactory;
            _logger = logger;
        }

        public void OnGet()
        {
            GetPrimaryDocumentData();
            GetPrimaryDocumentStatus();
            GetLinkedDocuments();
            GetAttachedLinkedDocuments();
            GetDatabaseDocuments();
        }

        public async Task<JsonResult> OnGetDatabaseSourceDocumentDataAsync(int sdocId)
        {
            DocumentAssessmentData sourceDocumentData = null;
            try
            {
                sourceDocumentData = await _dataServiceApiClient.GetAssessmentData(sdocId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed requesting DataService {DataServiceResource} with: SdocId: {SdocId};",
                    nameof(_dataServiceApiClient.GetAssessmentData),
                    sdocId);

                throw;
            }

            return new JsonResult(sourceDocumentData);
        }

        private void GetPrimaryDocumentData()
        {
            try
            {
                Assessment = _dbContext
                    .AssessmentData
                    .First(c => c.ProcessId == ProcessId);
            }
            catch (InvalidOperationException e)
            {
                // Log and throw, as we're unable to get assessment data
                e.Data.Add("OurMessage", "Unable to retrieve AssessmentData");
                Console.WriteLine(e);
                throw;
            }
        }

        private void GetLinkedDocuments()
        {
            try
            {
                LinkedDocuments = _dbContext
                    .LinkedDocument
                    .Where(c => c.ProcessId == ProcessId).ToList();
            }
            catch (ArgumentNullException e)
            {
                // Log and throw, as we're unable to get Linked Documents
                e.Data.Add("OurMessage", "Unable to retrieve Linked Documents");
                Console.WriteLine(e);
                throw;
            }
        }

        private void GetDatabaseDocuments()
        {
            try
            {
                DatabaseDocuments = _dbContext
                    .DatabaseDocumentStatus
                    .Where(c => c.ProcessId == ProcessId).ToList();

                foreach (var databaseDocumentStatus in DatabaseDocuments)
                {
                    if (databaseDocumentStatus.ContentServiceId.HasValue)
                        databaseDocumentStatus.ContentServiceUri =
                            _uriConfig.Value.BuildContentServiceUri(databaseDocumentStatus.ContentServiceId.Value);
                }

            }
            catch (ArgumentNullException e)
            {
                // Log and throw, as we're unable to get Database Documents
                e.Data.Add("OurMessage", "Unable to retrieve Database Documents");
                Console.WriteLine(e);
                throw;
            }
        }

        private void GetPrimaryDocumentStatus()
        {
            try
            {
                PrimaryDocumentStatus = _dbContext.PrimaryDocumentStatus.First(s => s.ProcessId == ProcessId);

                if (PrimaryDocumentStatus.ContentServiceId.HasValue)
                    PrimaryDocumentStatus.ContentServiceUri =
                        _uriConfig.Value.BuildContentServiceUri(PrimaryDocumentStatus.ContentServiceId.Value);
            }
            catch (InvalidOperationException e)
            {
                // Log that we're unable to get a Source Doc Status row
                e.Data.Add("OurMessage", "Unable to retrieve PrimaryDocumentStatus");
                Console.WriteLine(e);
            }
        }

        private void GetAttachedLinkedDocuments()
        {
            if (LinkedDocuments != null && LinkedDocuments.Any())
            {
                AttachedLinkedDocuments = LinkedDocuments.Where(l =>
                    !l.Status.Equals(LinkedDocumentRetrievalStatus.NotAttached.ToString(),
                        StringComparison.OrdinalIgnoreCase));

                foreach (var attachedLinkedDocument in AttachedLinkedDocuments)
                {
                    if (attachedLinkedDocument.ContentServiceId.HasValue)
                        attachedLinkedDocument.ContentServiceUri =
                            _uriConfig.Value.BuildContentServiceUri(attachedLinkedDocument.ContentServiceId.Value);
                }
            }
        }

        public async Task<IActionResult> OnPostAttachLinkedDocumentAsync(int linkedSdocId, int processId, Guid correlationId)
        {
            // Update DB first, as it is the one used for populating Attached secondary sources
            await SourceDocumentHelper.UpdateSourceDocumentStatus(
                                                                    _documentStatusFactory,
                                                                    processId,
                                                                    linkedSdocId,
                                                                    SourceDocumentRetrievalStatus.Started,
                                                                    SourceType.Linked, correlationId);

            var docRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                ProcessId = processId,
                SourceDocumentId = linkedSdocId,
                GeoReferenced = false,
                SourceType = SourceType.Linked
            };

            _logger.LogInformation("Publishing InitiateSourceDocumentRetrievalEvent: {InitiateSourceDocumentRetrievalEvent};", docRetrievalEvent.ToJSONSerializedString());
            await _eventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent), docRetrievalEvent);
            _logger.LogInformation("Published InitiateSourceDocumentRetrievalEvent: {InitiateSourceDocumentRetrievalEvent};", docRetrievalEvent.ToJSONSerializedString());

            return StatusCode(200);
        }

        /// <summary>
        /// Result of user clicking the Add Source from SDRA button
        /// </summary>
        /// <param name="sdocId"></param>
        /// <param name="docName"></param>
        /// <param name="docType"></param>
        /// <param name="processId"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAddSourceFromSdraAsync(int sdocId, string docName, string docType, int processId, Guid correlationId)
        {
            if (_dbContext.DatabaseDocumentStatus.Any(dds => dds.SdocId == sdocId && dds.ProcessId == processId))
            {
                // Method not allowed - Sdoc Id already added previously
                return StatusCode(405);
            }

            // Update DB first
            await SourceDocumentHelper.UpdateSourceDocumentStatus(
                _documentStatusFactory,
                processId,
                sdocId,
                SourceDocumentRetrievalStatus.Started,
                SourceType.Database, correlationId, docName, docType);

            var docRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                ProcessId = processId,
                SourceDocumentId = sdocId,
                GeoReferenced = false,
                SourceType = SourceType.Database
            };

            _logger.LogInformation("Publishing InitiateSourceDocumentRetrievalEvent: {InitiateSourceDocumentRetrievalEvent};", docRetrievalEvent.ToJSONSerializedString());
            await _eventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent), docRetrievalEvent);
            _logger.LogInformation("Published InitiateSourceDocumentRetrievalEvent: {InitiateSourceDocumentRetrievalEvent};", docRetrievalEvent.ToJSONSerializedString());

            return StatusCode(200);
        }
    }
}