using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Portal.Models;

namespace Portal.Pages.DbAssessment
{
    public class _OperatorsModel : PageModel
    {

        [DisplayName("Reviewer:")]
        public string Reviewer { get; set; }

        [DisplayName("Assessor:")]
        public string Assessor { get; set; }

        [DisplayName("Verifier:")]
        public string Verifier { get; set; }
        public SelectList Verifiers { get; set; }

        public void OnGet()
        {

        }
    }
}

