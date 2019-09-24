using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _CommentsModel : PageModel
    {
        public List<Comments> Comments { get; set; }

        public void OnGet()
        {

        }
    }
}