using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _RecordProductActionModel : PageModel
    {
        [DisplayName("Action:")]
        public bool Action { get; set; }
        [DisplayName("Change:")]
        public string Change { get; set; }

        public List<ProductAction> ProductActions { get; set; }

        public SelectList ImpactedProducts { get; set; }

        public SelectList ProductActionTypes { get; set; }


        public void OnGet()
        {

        }
    }
}