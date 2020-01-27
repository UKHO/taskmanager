using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public interface IPageValidationHelper
    {
        Task<bool> ValidateRecordProductAction(List<ProductAction> recordProductAction, List<string> validationErrorMessages);
        bool ValidateDataImpact();
    }
}