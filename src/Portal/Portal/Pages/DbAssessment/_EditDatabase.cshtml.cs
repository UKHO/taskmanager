using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _EditDatabaseModel : PageModel
    {
        [DisplayName("Select CARIS Workspace:")]
        public CachedHpdWorkspace CarisWorkspace { get; set; }
        public SelectList CarisWorkspaces { get; set; }

        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        public void OnGet()
        {
        }
    }
}