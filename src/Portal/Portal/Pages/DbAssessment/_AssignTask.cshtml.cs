using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Portal.Models;

namespace Portal.Pages.DbAssessment
{
    public class _AssignTaskModel : PageModel
    {
        public int AssignTaskId { get; set; }
        public int ProcessId { get; set; }

        [DisplayName("Assessor:")]
        public Assessor Assessor { get; set; }
        public SelectList Assessors { get; set; }

        [DisplayName("Verifier:")]
        public Verifier Verifier { get; set; }
        public SelectList Verifiers { get; set; }

        [DisplayName("Source Type:")]
        public SourceType SourceType { get; set; }
        public SelectList SourceTypes { get; set; }

        [DisplayName("Notes:")]
        public string Notes { get; set; }
        [DisplayName("Workspace Affected:")]
        public string WorkspaceAffected { get; set; }

        public int Ordinal { get; set; }

        public void OnGet()
        {
        }
    }
}