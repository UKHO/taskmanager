using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _SourceDocumentDetailsModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly IOptions<UriConfig> _uriConfig;

        [BindProperty(SupportsGet = true)] public int ProcessId { get; set; }
        public AssessmentData Assessment { get; set; }
        public SourceDocumentStatus SourceDocumentStatus { get; set; }
        public Uri SourceDocumentContentServiceUri { get; set; }


        public _SourceDocumentDetailsModel(WorkflowDbContext DbContext,
            IOptions<UriConfig> uriConfig)
        {
            _dbContext = DbContext;
            _uriConfig = uriConfig;
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
            return StatusCode(200);
            //TODO: Log!
        }
    }
}