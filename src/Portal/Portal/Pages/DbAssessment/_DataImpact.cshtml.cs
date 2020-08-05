using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    [Authorize]
    public class _DataImpactModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<_DataImpactModel> _logger;

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        public List<DataImpact> DataImpacts { get; set; }

        public DataImpact StsDataImpact { get; set; }

        public SelectList Usages { get; set; }

        public _DataImpactModel(WorkflowDbContext dbContext,
            ILogger<_DataImpactModel> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task OnGetAsync(int processId)
        {
            LogContext.PushProperty("ProcessId", processId);
            LogContext.PushProperty("PortalResource", nameof(OnGetAsync));

            ProcessId = processId;

            await PopulateUsages();
            await SetDataImpactFromDb();
            await SetStsDataImpactFromDb();
        }

        private async Task PopulateUsages()
        {
            var usages = await _dbContext.HpdUsage.OrderBy(u => u.SortIndex).ToListAsync();
            Usages = new SelectList(usages, nameof(HpdUsage.HpdUsageId), nameof(HpdUsage.Name));
        }

        private async Task SetDataImpactFromDb()
        {
            DataImpacts = await _dbContext.DataImpact
                .Where(di => di.ProcessId == ProcessId && !di.StsUsage).ToListAsync();

            if (DataImpacts == null || DataImpacts.Count == 0)
            {
                DataImpacts = new List<DataImpact>()
                {
                    new DataImpact()
                };
            }
        }

        private async Task SetStsDataImpactFromDb()
        {
            try
            {
                StsDataImpact = await _dbContext.DataImpact
                    .SingleOrDefaultAsync(di => di.ProcessId == ProcessId && di.StsUsage);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogError(exception,"Multiple STS Data Impact records found when only one is expected");
                throw;
            }

            StsDataImpact ??= new DataImpact();
        }
    }
}