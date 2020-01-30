using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;

namespace Portal.Pages.DbAssessment
{
    public class _EditDatabaseModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;

        [DisplayName("Select CARIS Workspace:")]
        public string SelectedCarisWorkspace { get; set; }

        [DisplayName("CARIS Project Name:")]
        public string ProjectName { get; set; }

        public _EditDatabaseModel(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(int processId)
        {
        }

        public async Task<JsonResult> OnGetWorkspacesAsync()
        {
            var cachedHpdWorkspaces = await _dbContext.CachedHpdWorkspace.Select(c => c.Name).ToListAsync();
            return new JsonResult(cachedHpdWorkspaces);
        }

        public async Task OnPostLaunchSourceEditorAsync(int processId)
        {
            var something = processId;
        }
    }
}