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
        public Assessor Assessor { get; set; }

        [DisplayName("Verifier:")]
        public Verifier Verifier { get; set; }
        public SelectList Verifiers { get; set; }

        public void OnGet()
        {

        }
    }
}

