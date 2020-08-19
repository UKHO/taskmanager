using DbUpdatePortal.Enums;
using DbUpdateWorkflowDatabase.EF.Models;
using System;
using System.Collections.Generic;

namespace DbUpdatePortal.Helpers
{
    public interface IPageValidationHelper
    {
        public bool ValidateNewTaskPage(TaskRole taskRole, string taskName, string chartingArea, string updateType, string productAction,
            List<string> validationErrorMessages);

        public bool ValidateForCompletion(string assignedUser, string username, DbUpdateTaskStageType stageType,
            TaskRole role, DateTime? targetDate,
            List<string> validationErrorMessages);

        public bool ValidateForRework(string assignedUser, string username,
            List<string> validationErrorMessages);

        public bool ValidateForCompleteWorkflow(string assignedUser, string username,
            List<string> validationErrorMessages);

        public bool ValidateWorkflowPage(TaskRole role, string productAction, DateTime? targetDate,
            List<string> validationErrorMessages);
    }
}
