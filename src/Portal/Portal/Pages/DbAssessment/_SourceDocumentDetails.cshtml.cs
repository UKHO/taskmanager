using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Factories;
using Common.Factories.Interfaces;
using Common.Messages.Enums;
using Common.Messages.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using Portal.HttpClients;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _SourceDocumentDetailsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOptions<UriConfig> _uriConfig;
        private readonly IEventServiceApiClient _eventServiceApiClient;
        private readonly IDocumentStatusFactory _documentStatusFactory;

        [BindProperty(SupportsGet = true)] public int ProcessId { get; set; }
        public AssessmentData Assessment { get; set; }
        public IEnumerable<LinkedDocument> AttachedLinkedDocuments { get; set; }
        public PrimaryDocumentStatus PrimaryDocumentStatus { get; set; }
        public Uri PrimaryDocumentContentServiceUri { get; set; }


        public _SourceDocumentDetailsModel(WorkflowDbContext DbContext,
            IOptions<UriConfig> uriConfig, IEventServiceApiClient eventServiceApiClient, IDocumentStatusFactory documentStatusFactory)
        {
            _dbContext = DbContext;
            _uriConfig = uriConfig;
            _eventServiceApiClient = eventServiceApiClient;
            _documentStatusFactory = documentStatusFactory;
        }

        public void OnGet()
        {
            GetPrimaryDocumentData();
            GetPrimaryDocumentStatus();
            GetAttachedLinkedDocuments();
        }

        private void GetPrimaryDocumentData()
        {
            try
            {
                Assessment = _dbContext
                    .AssessmentData
                    .Include(a => a.LinkedDocuments)
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

        private void GetPrimaryDocumentStatus()
        {
            try
            {
                PrimaryDocumentStatus = _dbContext.PrimaryDocumentStatus.First(s => s.ProcessId == ProcessId);

                if (PrimaryDocumentStatus.ContentServiceId.HasValue)
                    PrimaryDocumentContentServiceUri =
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
            if (Assessment.LinkedDocuments != null && Assessment.LinkedDocuments.Count > 0)
            {
                AttachedLinkedDocuments = Assessment.LinkedDocuments.Where(l =>
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

        public async Task<IActionResult> OnPostAttachLinkedDocumentAsync(int linkedSdocId)
        {
            // Update DB first, as it is the one used for populating Attached secondary sources
            await SourceDocumentHelper.UpdateSourceDocumentStatus(
                                                                    _documentStatusFactory,
                                                                    ProcessId,
                                                                    linkedSdocId,
                                                                    SourceDocumentRetrievalStatus.Started,
                                                                    SourceDocumentType.Linked);

            var docRetrievalEvent = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = PrimaryDocumentStatus.CorrelationId.HasValue
                    ? PrimaryDocumentStatus.CorrelationId.Value
                    : Guid.NewGuid(),
                ProcessId = ProcessId,
                SourceDocumentId = linkedSdocId,
                GeoReferenced = false,
                DocumentType = SourceDocumentType.Linked
            };

            await _eventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent),
                docRetrievalEvent);

            return StatusCode(200);
            ////TODO: Log!
        }
    }
}