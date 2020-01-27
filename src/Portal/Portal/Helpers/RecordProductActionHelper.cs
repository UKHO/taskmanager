using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class RecordProductActionHelper : IRecordProductActionHelper
    {
        private readonly HpdDbContext _hpdDbContext;

        public RecordProductActionHelper(HpdDbContext hpdDbContext)
        {
            _hpdDbContext = hpdDbContext;
        }
        public async Task<bool> ValidateRecordProductAction(List<ProductAction> recordProductAction, List<string> validationErrorMessages)
        {
            bool isValid = true;

            if (recordProductAction != null && recordProductAction.Count > 0)
            {
                // Check at least one entry populated
                if (recordProductAction.Any(r => 
                    string.IsNullOrWhiteSpace(r.ImpactedProduct) 
                    && (r.ProductActionType == null || string.IsNullOrWhiteSpace(r.ProductActionType.Name))))
                {
                    validationErrorMessages.Add($"Record Product Action: Please ensure impacted product is fully populated");
                    return false;
                } 

                foreach (var productAction in recordProductAction)
                {
                    // Check for existing impacted products
                    var isExist = await _hpdDbContext.CarisProducts.AnyAsync(p =>
                        p.ProductStatus.Equals("Active", StringComparison.InvariantCultureIgnoreCase) &&
                        p.TypeKey.Equals("ENC", StringComparison.InvariantCultureIgnoreCase) &&
                        p.ProductName.Equals(productAction.ImpactedProduct, StringComparison.InvariantCultureIgnoreCase));

                    if (!isExist)
                    {
                        validationErrorMessages.Add($"Record Product Action: Impacted product {productAction.ImpactedProduct} does not exist");
                        isValid = false;
                    }
                }

                if (recordProductAction.GroupBy(p => p.ImpactedProduct)
                    .Where(g => g.Count() > 1)
                    .Select(y => y.Key).Any())
                {
                    validationErrorMessages.Add("Record Product Action: More than one of the same Impacted Products selected");
                    isValid = false;
                }
            }

            return isValid;
        }

    }
}
