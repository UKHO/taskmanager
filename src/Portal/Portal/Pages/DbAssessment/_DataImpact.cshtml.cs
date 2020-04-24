using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    public class _DataImpactModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        public List<DataImpact> DataImpacts { get; set; }

        public SelectList Usages { get; set; }

        public _DataImpactModel(WorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(int processId)
        {
            ProcessId = processId;

            await PopulateUsages();
            await SetDataImpactFromDb();
        }

        private async Task PopulateUsages()
        {
            var usages = await _dbContext.HpdUsage.OrderBy(u => u.SortIndex).ToListAsync();
            Usages = new SelectList(usages, nameof(HpdUsage.HpdUsageId), nameof(HpdUsage.Name));
        }

        private async Task SetDataImpactFromDb()
        {

            DataImpacts = await _dbContext.DataImpact.Where(di => di.ProcessId == ProcessId).ToListAsync();

            if (DataImpacts == null || DataImpacts.Count == 0)
            {
                DataImpacts = new List<DataImpact>()
                {
                    new DataImpact()
                };
            }
        }
    }
}