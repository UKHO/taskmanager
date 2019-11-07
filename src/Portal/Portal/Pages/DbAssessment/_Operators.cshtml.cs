using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Portal.Pages.DbAssessment
{
    public class _OperatorsModel : PageModel
    {

        [DisplayName("Work Manager:")]
        public string WorkManager { get; set; }

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

