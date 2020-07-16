using Common.Factories;
using Common.Factories.Interfaces;
using Common.Helpers;
using Common.Messages.Enums;
using Common.Messages.Events;
using DataServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.BusinessLogic;
using Portal.Configuration;
using Portal.HttpClients;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;
using LinkedDocument = WorkflowDatabase.EF.Models.LinkedDocument;

namespace Portal.Pages.DbAssessment
{
    public class _SourceDocumentDetailsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly IWorkflowBusinessLogicService _workflowBusinessLogicService;
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
            IOptions<UriConfig> uriConfig,
            IWorkflowBusinessLogicService workflowBusinessLogicService,
            IEventServiceApiClient eventServiceApiClient,
            IDataServiceApiClient dataServiceApiClient, IDocumentStatusFactory documentStatusFactory,
            ILogger<_SourceDocumentDetailsModel> logger)
        {
            _dbContext = dbContext;
            _uriConfig = uriConfig;
            _workflowBusinessLogicService = workflowBusinessLogicService;
            _eventServiceApiClient = eventServiceApiClient;
            _dataServiceApiClient = dataServiceApiClient;
            _documentStatusFactory = documentStatusFactory;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            LogContext.PushProperty("ProcessId", ProcessId);
            LogContext.PushProperty("PortalResource", nameof(OnGetAsync));

            _logger.LogInformation("Entering {PortalResource} for _SourceDocumentDetailsModel with: ProcessId: {ProcessId};");

            await GetAssessmentDataAsync();
            await GetPrimaryDocumentStatusAsync();
            await GetLinkedDocumentsAsync();
            GetAttachedLinkedDocuments();
            await GetDatabaseDocumentsAsync();
        }

        public async Task<JsonResult> OnGetDatabaseSourceDocumentDataAsync(int sdocId)
        {
            LogContext.PushProperty("ProcessId", ProcessId);
            LogContext.PushProperty("SourceDocumentId", sdocId);
            LogContext.PushProperty("PortalResource", nameof(OnGetDatabaseSourceDocumentDataAsync));

            _logger.LogInformation("Entering {PortalResource} for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; and SourceDocumentId {SourceDocumentId}");

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

        private async Task GetAssessmentDataAsync()
        {
            _logger.LogInformation("Getting Assessment data for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; and SourceDocumentId {SourceDocumentId}");

            try
            {
                Assessment = await _dbContext
                    .AssessmentData
                    .FirstAsync(c => c.ProcessId == ProcessId);
            }
            catch (InvalidOperationException e)
            {
                // Log and throw, as we're unable to get assessment data
                e.Data.Add("OurMessage", "Unable to retrieve AssessmentData");
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task GetLinkedDocumentsAsync()
        {
            _logger.LogInformation("Getting Primary linked documents data for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; and SourceDocumentId {SourceDocumentId}");

            try
            {
                LinkedDocuments = await _dbContext
                    .LinkedDocument
                    .Where(c => c.ProcessId == ProcessId)
                    .ToListAsync();
            }
            catch (ArgumentNullException e)
            {
                // Log and throw, as we're unable to get Linked Documents
                e.Data.Add("OurMessage", "Unable to retrieve Linked Documents");
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task GetDatabaseDocumentsAsync()
        {
            _logger.LogInformation("Getting Database documents data for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; and SourceDocumentId {SourceDocumentId}");

            try
            {
                DatabaseDocuments = await _dbContext
                    .DatabaseDocumentStatus
                    .Where(c => c.ProcessId == ProcessId)
                    .ToListAsync();

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

        private async Task GetPrimaryDocumentStatusAsync()
        {
            _logger.LogInformation("Getting Primary document status data for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; and SourceDocumentId {SourceDocumentId}");

            try
            {
                PrimaryDocumentStatus = await _dbContext.PrimaryDocumentStatus.FirstAsync(s => s.ProcessId == ProcessId);

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
            _logger.LogInformation("Getting 'Attached' linked document data for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; and SourceDocumentId {SourceDocumentId}");

            if (LinkedDocuments != null && LinkedDocuments.Any())
            {
                AttachedLinkedDocuments = LinkedDocuments.Where(l =>
                    l.Status != SourceDocumentRetrievalStatus.NotAttached.ToString());

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
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("LinkedSdocId", linkedSdocId);
            LogContext.PushProperty("PortalResource", nameof(OnPostAttachLinkedDocumentAsync));

            _logger.LogInformation("Entering {PortalResource} for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; LinkedSdocId: {LinkedSdocId}");

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} is readonly, cannot attach linked document");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} is readonly, cannot attach linked document");
                throw appException;
            }

            // Update DB first, as it is the one used for populating Attached secondary sources
            _logger.LogInformation("Updating document status in database for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; LinkedSdocId: {LinkedSdocId}");

            await SourceDocumentHelper.UpdateSourceDocumentStatus(
                                                                    _documentStatusFactory,
                                                                    processId,
                                                                    linkedSdocId,
                                                                    SourceDocumentRetrievalStatus.Started,
                                                                    SourceType.Linked);

            var docRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                ProcessId = processId,
                SourceDocumentId = linkedSdocId,
                GeoReferenced = false,
                SourceType = SourceType.Linked,
                SdocRetrievalId = Guid.NewGuid()
            };

            LogContext.PushProperty("InitiateSourceDocumentRetrievalEvent", nameof(InitiateSourceDocumentRetrievalEvent));
            LogContext.PushProperty("Message", docRetrievalEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing InitiateSourceDocumentRetrievalEvent: {InitiateSourceDocumentRetrievalEvent}; Message: {Message}");
            await _eventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent), docRetrievalEvent);
            _logger.LogInformation("Published InitiateSourceDocumentRetrievalEvent: {InitiateSourceDocumentRetrievalEvent}; Message: {Message}");

            return StatusCode(200);
        }

        public async Task<IActionResult> OnPostDetachLinkedDocumentAsync(int linkedSdocId, int processId)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("LinkedSdocId", linkedSdocId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDetachLinkedDocumentAsync));

            _logger.LogInformation("Entering {PortalResource} for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; LinkedSdocId: {LinkedSdocId}");

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} is readonly, cannot attach linked document");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} is readonly, cannot attach linked document");
                throw appException;
            }

            _logger.LogInformation("Updating document status in database for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; LinkedSdocId: {LinkedSdocId}");

            await SourceDocumentHelper.UpdateSourceDocumentStatus(
                                                                    _documentStatusFactory,
                                                                    processId,
                                                                    linkedSdocId,
                                                                    SourceDocumentRetrievalStatus.NotAttached,
                                                                    SourceType.Linked);

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
        public async Task<IActionResult> OnPostAddSourceFromSdraAsync(int sdocId, int processId, Guid correlationId)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("SdocId", sdocId);
            LogContext.PushProperty("PortalResource", nameof(OnPostAddSourceFromSdraAsync));

            _logger.LogInformation("Entering {PortalResource} for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; SdocId: {SdocId}");

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} is readonly, cannot add source from SDRA");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} is readonly, cannot add source from SDRA");
                throw appException;
            }

            _logger.LogInformation("Updating document status in database for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; SdocId: {SdocId}");

            if (_dbContext.DatabaseDocumentStatus.Any(dds => dds.SdocId == sdocId && dds.ProcessId == processId))
            {
                // Method not allowed - Sdoc Id already added previously
                return StatusCode(405);
            }

            // Update DB first, as it is the one used for populating Attached sources
            var sourceDocumentData = await _dataServiceApiClient.GetAssessmentData(sdocId);

            await _dbContext.DatabaseDocumentStatus.AddAsync(new DatabaseDocumentStatus()
            {
                ProcessId = processId,
                SdocId = sourceDocumentData.SdocId,
                RsdraNumber = sourceDocumentData.SourceName,
                SourceDocumentName = sourceDocumentData.Name,
                ReceiptDate = sourceDocumentData.ReceiptDate,
                SourceDocumentType = sourceDocumentData.DocumentType,
                SourceNature = sourceDocumentData.DocumentNature,
                Datum = sourceDocumentData.Datum,
                Status = SourceDocumentRetrievalStatus.Started.ToString(),
                Created = DateTime.Now,

            });

            await _dbContext.SaveChangesAsync();

            var docRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = correlationId,
                ProcessId = processId,
                SourceDocumentId = sdocId,
                GeoReferenced = false,
                SourceType = SourceType.Database,
                SdocRetrievalId = Guid.NewGuid()
            };

            LogContext.PushProperty("InitiateSourceDocumentRetrievalEvent", nameof(InitiateSourceDocumentRetrievalEvent));
            LogContext.PushProperty("Message", docRetrievalEvent.ToJSONSerializedString());

            _logger.LogInformation("Publishing InitiateSourceDocumentRetrievalEvent: {InitiateSourceDocumentRetrievalEvent}; Message: {Message}");
            await _eventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent), docRetrievalEvent);
            _logger.LogInformation("Published InitiateSourceDocumentRetrievalEvent: {InitiateSourceDocumentRetrievalEvent}; Message: {Message}");

            return StatusCode(200);
        }


        public async Task<IActionResult> OnPostDetachDatabaseDocumentAsync(int sdocId, int processId)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("SdocId", sdocId);
            LogContext.PushProperty("PortalResource", nameof(OnPostDetachDatabaseDocumentAsync));

            _logger.LogInformation("Entering {PortalResource} for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; SdocId: {SdocId}");

            var isWorkflowReadOnly = await _workflowBusinessLogicService.WorkflowIsReadOnlyAsync(processId);

            if (isWorkflowReadOnly)
            {
                var appException = new ApplicationException($"Workflow Instance for {nameof(processId)} {processId} is readonly, cannot attach linked document");
                _logger.LogError(appException,
                    "Workflow Instance for ProcessId {ProcessId} is readonly, cannot attach linked document");
                throw appException;
            }
            
            _logger.LogInformation("Updating document status in database for _SourceDocumentDetailsModel with: ProcessId: {ProcessId}; SdocId: {SdocId}");

            var databaseDocument =
                await _dbContext.DatabaseDocumentStatus.FirstOrDefaultAsync(dds =>
                    dds.ProcessId == processId && dds.SdocId == sdocId);

            if (databaseDocument == null)
            {
                _logger.LogWarning("Failed to find source document attached from SDRA source; ProcessId: {ProcessId}; SdocId: {SdocId}");
                throw new ApplicationException($"Failed to find source document attached from SDRA source; ProcessId: {processId}; SdocId: {sdocId}");
            }

            _dbContext.DatabaseDocumentStatus.Remove(databaseDocument);
            await _dbContext.SaveChangesAsync();

            return StatusCode(200);
        }
    }
}
