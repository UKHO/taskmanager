using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Pages.DbAssessment
{
    [Authorize]
    public class _RecordProductActionModel : PageModel
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ITaskDataHelper _taskDataHelper;

        [BindProperty(SupportsGet = true)]
        public int ProcessId { get; set; }

        [DisplayName("Action:")]
        public bool ProductActioned { get; set; }

        [DisplayName("Change:")]
        public string ProductActionChangeDetails { get; set; }

        public List<ProductAction> ProductActions { get; set; }

        public SelectList ProductActionTypes { get; set; }

        public _RecordProductActionModel(WorkflowDbContext dbContext, ITaskDataHelper taskDataHelper)
        {
            _dbContext = dbContext;
            _taskDataHelper = taskDataHelper;
        }

        public async Task OnGetAsync(int processId, string taskStage)
        {
            ProcessId = processId;

            await PopulateProductActionTypes();
            await SetProductActionFromDb();
            await SetProductActionDataFromDb(taskStage);
        }

        public async Task<JsonResult> OnGetImpactedProductsAsync()
        {
            var cachedHpdEncProduct = await _dbContext.CachedHpdEncProduct.Select(c => c.Name).ToListAsync();
            return new JsonResult(cachedHpdEncProduct);
        }

        private async Task PopulateProductActionTypes()
        {
            var productActionTypes = await _dbContext.ProductActionType.ToListAsync();
            ProductActionTypes = new SelectList(productActionTypes, nameof(ProductActionType.ProductActionTypeId), nameof(ProductActionType.Name));
        }

        private async Task SetProductActionFromDb()
        {
            ProductActions = await _dbContext.ProductAction
                .Include(p => p.ProductActionType)
                .Where(pa => pa.ProcessId == ProcessId)
                .ToListAsync();

            if (ProductActions == null || ProductActions.Count == 0)
            {
                ProductActions = new List<ProductAction>()
                {
                    new ProductAction()
                };
            }

        }

        private async Task SetProductActionDataFromDb(string taskStage)
        {
            var dbAssessmentData = await _taskDataHelper.GetProductActionData(taskStage, ProcessId);

            if (dbAssessmentData != null)
            {
                ProductActioned = dbAssessmentData.ProductActioned;
                ProductActionChangeDetails = dbAssessmentData.ProductActionChangeDetails;
            }

        }
    }
}