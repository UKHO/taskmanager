using DbUpdateWorkflowDatabase.EF.Models;
using System.Collections.Generic;

namespace DbUpdatePortal.Helpers
{
    public interface IPageValidationHelper
    {
        public bool ValidateNewTaskPage(TaskRole taskRole, string taskName, string chartingArea, string updateType, string productAction,
            List<string> validationErrorMessages);
    }
}
