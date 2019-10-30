using System;
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
        public PrimaryDocumentStatus PrimaryDocumentStatus { get; set; }
        public Uri SourceDocumentContentServiceUri { get; set; }


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

            try
            {
                PrimaryDocumentStatus = _dbContext.PrimaryDocumentStatus.First(s => s.ProcessId == ProcessId);

                if (PrimaryDocumentStatus.ContentServiceId != null)
                    SourceDocumentContentServiceUri =
                        _uriConfig.Value.BuildContentServiceUri(PrimaryDocumentStatus.ContentServiceId.Value);

            }
            catch (InvalidOperationException e)
            {
                // Log that we're unable to get a Source Doc Status row
                e.Data.Add("OurMessage", "Unable to retrieve PrimaryDocumentStatus");
                Console.WriteLine(e);
            }
        }

        public async Task<IActionResult> OnPostAttachLinkedDocumentAsync(int linkedSdocId)
        {
            // TODO: Update DB here
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