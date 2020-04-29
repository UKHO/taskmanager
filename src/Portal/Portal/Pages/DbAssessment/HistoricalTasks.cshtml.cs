using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.Models;

namespace Portal.Pages.DbAssessment
{
    public class HistoricalTasksModel : PageModel
    {

        [BindProperty(SupportsGet = true)]
        public HistoricalTasksSearchParameters SearchParameters { get; set; }

        public List<string> ErrorMessages { get; set; }

        public void OnGet()
        {
            ErrorMessages = new List<string>();
        }

        public void OnPost()
        {
            ErrorMessages = new List<string>()
            {
                "Error1",
                "Error2"
            };

        }
    }
}