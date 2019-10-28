using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _SourceDocumentDetailsModel : PageModel
    {
        public int ProcessId { get; set; }
        public AssessmentData Assessment { get; set; }
        public SourceDocumentStatus SourceDocumentStatus { get; set; }
        public Uri SourceDocumentContentServiceUri { get; set; }

        public void OnGet()
        {

        }
    }
}