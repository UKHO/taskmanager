using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Portal.Pages.DbAssessment
{
    public class _DataImpactModel : PageModel
    {
        [DisplayName("Usage")]
        public string Usage { get; set; }

        [DisplayName("Edited")]
        public string Edited { get; set; }
        public SelectList ImpactedProducts { get; set; }

        [DisplayName("Comments")]
        public string Comments { get; set; }


        public void OnGet()
        {
        }
    }
}