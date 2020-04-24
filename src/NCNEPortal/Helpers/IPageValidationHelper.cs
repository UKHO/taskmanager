using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Collections.Generic;

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

        public bool ValidateNewTaskPage(TaskRole taskRole, string workflowType, string chartType,
            List<string> validationErrorMessages);

        public bool ValidateForCompletion(int processId, int stageId, string assignedUser, string userName,
            List<string> validationErrorMessages);
    }
}
