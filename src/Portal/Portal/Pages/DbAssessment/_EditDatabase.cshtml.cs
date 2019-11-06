using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Portal.Pages.DbAssessment
{
    public class _EditDatabaseModel : PageModel
    {
        [DisplayName("Select CARIS Workspace:")]
        public CarisWorkspace CarisWorkspace { get; set; }
        public SelectList CarisWorkspaces { get; set; }

        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        public void OnGet()
        {
        }
    }
}