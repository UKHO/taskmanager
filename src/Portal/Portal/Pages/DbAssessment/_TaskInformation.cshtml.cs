using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Portal.Pages.DbAssessment
{
    public class _TaskInformationModel : PageModel
    {
        [DisplayName("Process Id:")]
        public int ProcessId { get; set; }

        [DisplayName("DM End Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime DmEndDate { get; set; }

        [DisplayName("DM Receipt Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime DmReceiptDate { get; set; }

        [DisplayName("Effective Receipt Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime EffectiveReceiptDate { get; set; }

        [DisplayName("External End Date:")]
        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime ExternalEndDate { get; set; }

        [DisplayName("On Hold:")]
        public int OnHold { get; set; }

        [DisplayName("ION:")]
        public string Ion { get; set; }

        [DisplayName("Activity Code:")]
        public string ActivityCode { get; set; }

        [DisplayName("Source Category:")]
        public SourceCategory SourceCategory { get; set; }

        public SelectList SourceCategories { get; set; }

        public void OnGet()
        {

        }
    }
}