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
            string chartType,
            (bool SentTo3Ps, DateTime? SendDate3ps, DateTime? ExpectedReturnDate3ps, DateTime? ActualReturnDate3ps)
                threePsInfo,
            List<string> validationErrorMessages);


    }
}
