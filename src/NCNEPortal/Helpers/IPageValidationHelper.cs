using NCNEPortal.Enums;
using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Collections.Generic;

namespace NCNEPortal.Helpers
{
    public interface IPageValidationHelper
    {
        bool ValidateWorkflowPage(TaskRole taskRole, string workflowType,
            string chartNo,
            (bool SentTo3Ps, DateTime? SendDate3ps, DateTime? ExpectedReturnDate3ps, DateTime? ActualReturnDate3ps)
                threePsInfo,
            List<string> validationErrorMessages);

        public bool ValidateNewTaskPage(TaskRole taskRole, string workflowType, string chartType,
            List<string> validationErrorMessages, string chartNo = "");

        public bool ValidateForCompletion(string assignedUser, string userName, NcneTaskStageType stageType,
            TaskRole role, DateTime? publicationDate,
            DateTime? repromatDate,
            int dating,
            string chartType, List<string> validationErrorMessages);

        public bool ValidateForRework(string assignedUser, string userName, List<string> validationErrorMessages);

        public bool ValidateForCompleteWorkflow(string assignedUser, string userName, List<string> validationErrorMessages);

        public bool ValidateForPublishCarisChart(bool threePs, DateTime? actualReturnDate3Ps,
                                                 int currentStageTypeId, string formsStatus, List<string> validationErrorMessages);
    }
}
