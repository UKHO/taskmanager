using System;
using System.Linq;
using System.Threading.Tasks;
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

        [BindProperty(SupportsGet = true)] public int ProcessId { get; set; }
        public AssessmentData Assessment { get; set; }
        public SourceDocumentStatus SourceDocumentStatus { get; set; }
        public Uri SourceDocumentContentServiceUri { get; set; }


        public _SourceDocumentDetailsModel(WorkflowDbContext DbContext,
            IOptions<UriConfig> uriConfig, IEventServiceApiClient eventServiceApiClient)
        {
            _dbContext = DbContext;
            _uriConfig = uriConfig;
            _eventServiceApiClient = eventServiceApiClient;
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
                SourceDocumentStatus = _dbContext.SourceDocumentStatus.First(s => s.ProcessId == ProcessId);

                if (SourceDocumentStatus.ContentServiceId != null)
                    SourceDocumentContentServiceUri =
                        _uriConfig.Value.BuildContentServiceUri(SourceDocumentStatus.ContentServiceId.Value);

            }
            catch (InvalidOperationException e)
            {
                // Log that we're unable to get a Source Doc Status row
                e.Data.Add("OurMessage", "Unable to retrieve SourceDocumentStatus");
                Console.WriteLine(e);
            }
        }

        public async Task<IActionResult> OnPostAttachLinkedDocumentAsync(int linkedSdocId)
        {
            // TODO: Update DB here
            var docType = new InitiateSourceDocumentRetrievalEvent
            {
                CorrelationId = SourceDocumentStatus.CorrelationId.HasValue
                    ? SourceDocumentStatus.CorrelationId.Value
                    : Guid.NewGuid(),
                ProcessId = ProcessId,
                SourceDocumentId = linkedSdocId,
                GeoReferenced = false,
                DocumentType = SourceDocumentType.Linked
            };

            // TODO: work out how to get the event body in ere
            await _eventServiceApiClient.PostEvent(nameof(InitiateSourceDocumentRetrievalEvent));

            return StatusCode(200);
            //TODO: Log!
        }
    }
}