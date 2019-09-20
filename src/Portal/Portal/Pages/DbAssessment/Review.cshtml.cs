using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Portal.Pages.DbAssessment
{
    public class ReviewModel : PageModel
    {
        public _TaskInformationModel TaskInformationModel { get; set; }
        public void OnGet()
        {
            TaskInformationModel = new _TaskInformationModel();
            TaskInformationModel.ProcessId = 98;
            TaskInformationModel.DmEndDate = DateTime.Now;
            TaskInformationModel.DmReceiptDate = DateTime.Now;
            TaskInformationModel.EffectiveReceiptDate = DateTime.Now;
            TaskInformationModel.ExternalEndDate = DateTime.Now;
            TaskInformationModel.OnHold = 4;
            TaskInformationModel.Ion = "2929";
            TaskInformationModel.ActivityCode = "1272";
            TaskInformationModel.SourceCategory = new SourceCategory() { SourceCategoryId = 1, Name = "zzzzz" };

            TaskInformationModel.SourceCategories = new SelectList(new List<SourceCategory>()
                {
                    new SourceCategory() {SourceCategoryId = 0, Name = "asdas"},
                    new SourceCategory() {SourceCategoryId = 1, Name = "zzzzz"}
                }, "SourceCategoryId", "Name");
        }
    }
}