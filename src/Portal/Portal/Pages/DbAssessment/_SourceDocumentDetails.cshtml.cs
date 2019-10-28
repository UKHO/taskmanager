using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _SourceDocumentDetailsModel : PageModel
    {
        public int ProcessId { get; set; }
        public List<AssessmentData> Assessments { get; set; }
        public SourceDocumentStatus SourceDocumentStatus { get; set; }
        public string ContentServiceUri { get; set; }

        public void OnGet()
        {

        }
    }
}