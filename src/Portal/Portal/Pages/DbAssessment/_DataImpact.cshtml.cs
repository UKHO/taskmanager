using System.Collections.Generic;
using System.ComponentModel;
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
            await SetDataImpactModelDummyData();
        }

        private async Task PopulateUsages()
        {
            var usages = await _dbContext.HpdUsage.ToListAsync();
            Usages = new SelectList(usages, nameof(HpdUsage.HpdUsageId), nameof(HpdUsage.Name));
        }

        private async Task SetDataImpactModelDummyData()
        {
            var usages = await _dbContext.HpdUsage.ToListAsync();

            DataImpacts = new List<DataImpact>
            {
                new DataImpact()
                {
                    DataImpactId = 1,
                    Comments = "Test1", 
                    HpdUsageId = 1,  
                    HpdUsage = usages.FirstOrDefault(h =>h.HpdUsageId == 1), 
                    Edited = false,
                    ProcessId = ProcessId, 
                    Verified = false
                },

                new DataImpact()
                {
                    DataImpactId = 2,
                    Comments = "Test2",
                    HpdUsageId = 2,
                    HpdUsage = usages.FirstOrDefault(h =>h.HpdUsageId == 2),
                    Edited = false,
                    ProcessId = ProcessId,
                    Verified = false
                }
            };
        }
    }
}