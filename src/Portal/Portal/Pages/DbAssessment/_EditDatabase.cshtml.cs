using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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

        public List<string> CachedHpdWorkspaces { get; set; }

        public _EditDatabaseModel(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(int processId)
        {
            await PopulateHpdWorkspaces();
            SetEditDatabaseModel();
        }

        private async Task PopulateHpdWorkspaces()
        {
            CachedHpdWorkspaces = await _dbContext.CachedHpdWorkspace.Select(c => c.Name).ToListAsync();
        }

        private void SetEditDatabaseModel()
        {
            SelectedCarisWorkspace = (CachedHpdWorkspaces == null || CachedHpdWorkspaces.Count == 0) ? "" : CachedHpdWorkspaces.First();
            ProjectName = "Testing Project";
        }
    }
}