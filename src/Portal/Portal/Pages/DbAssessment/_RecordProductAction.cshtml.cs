using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Portal.Pages.DbAssessment
{
    public class _RecordProductActionModel : PageModel
    {
        [DisplayName("Action:")]
        public bool Action { get; set; }
        [DisplayName("Change:")]
        public string Change { get; set; }

        [DisplayName("Impacted Product:")]
        public ImpactedProduct ImpactedProduct { get; set; }
        public SelectList ImpactedProducts { get; set; }

        [DisplayName("Product Action Type:")]
        public ProductActionType ProductActionType { get; set; }
        public SelectList ProductActionTypes { get; set; }


        public void OnGet()
        {
        }
    }
}