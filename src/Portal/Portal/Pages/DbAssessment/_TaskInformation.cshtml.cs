using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Portal.Pages.DbAssessment
{
    public class _TaskInformationModel : PageModel
    {
        public int ProcessId { get; set; }
        public DateTime DmEndDate { get; set; }
        public DateTime DmReceiptDate { get; set; }
        public DateTime EffectiveReceiptDate { get; set; }
        public DateTime ExternalEndDate { get; set; }
        public int OnHold { get; set; }
        public string Ion { get; set; }
        public string ActivityCode { get; set; }
        public SourceCategory SourceCategory { get; set; }

        public SelectList SourceCategories { get; set; }

        public void OnGet()
        {
            
        }
    }
}