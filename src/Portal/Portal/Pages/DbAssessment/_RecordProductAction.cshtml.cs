using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Portal.Helpers;
using Portal.Models;
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

        public SelectList ImpactedProducts { get; set; }

        public SelectList ProductActionTypes { get; set; }

        public _RecordProductActionModel(WorkflowDbContext dbContext, ITaskDataHelper taskDataHelper)
        {
            _dbContext = dbContext;
            _taskDataHelper = taskDataHelper;
        }


        public async Task OnGetAsync(int processId, string taskStage)
        {
            ProcessId = processId;

            SetHpdProducts();
            await PopulateProductActionTypes();
            await SetProductActionFromDb();
            await SetProductActionDataFromDb(taskStage);
        }

        private async Task PopulateProductActionTypes()
        {
            var productActionTypes = await _dbContext.ProductActionType.ToListAsync();
            ProductActionTypes = new SelectList(productActionTypes, nameof(ProductActionType.ProductActionTypeId), nameof(ProductActionType.Name));
        }

        private void SetHpdProducts()
        {
            //TODO: Change to read from real data.

            ImpactedProducts = new SelectList(
                new List<ImpactedProduct>
                {
                    new ImpactedProduct {ProductId = 0, Product = "Select..."},
                    new ImpactedProduct {ProductId = 1, Product = "GB123456"},
                    new ImpactedProduct {ProductId = 2, Product = "GB111222"},
                    new ImpactedProduct {ProductId = 3, Product = "GB987651"}
                }, "ProductId", "Product");

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