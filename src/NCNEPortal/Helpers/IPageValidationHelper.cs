using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NCNEPortal.Helpers
{
    public interface IPageValidationHelper
    {
        bool ValidateWorkflowPage(TaskRole taskRole,
                                        DateTime? publicationDate,
                                        DateTime? repromatDate,
                                        int dating,
                                        String chartType,
                                        List<String> validationErrorMessages);


    }
}
