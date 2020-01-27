using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public interface IRecordProductActionHelper
    {
        Task<bool> ValidateRecordProductAction(List<ProductAction> recordProductAction, List<string> validationErrorMessages);
    }
}