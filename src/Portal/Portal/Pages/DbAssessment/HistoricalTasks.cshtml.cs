using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Portal.Models;

namespace Portal.Pages.DbAssessment
{
    public class HistoricalTasksModel : PageModel
    {

        [BindProperty(SupportsGet = true)]
        public HistoricalTasksSearchParameters SearchParameters { get; set; }   
        public void OnGet()
        {

        }
    }
}