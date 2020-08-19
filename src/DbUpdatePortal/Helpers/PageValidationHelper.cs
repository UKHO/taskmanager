using DbUpdatePortal.Auth;
using DbUpdatePortal.Enums;
using DbUpdateWorkflowDatabase.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbUpdatePortal.Helpers
{
    public class PageValidationHelper : IPageValidationHelper
    {
        private readonly IDbUpdateUserDbService _dbUpdateUserDbService;

        public PageValidationHelper(IDbUpdateUserDbService dbUpdateUserDbService)
        {
            _dbUpdateUserDbService = dbUpdateUserDbService;
        }

        public bool ValidateNewTaskPage(TaskRole taskRole, string taskName, string chartingArea, string updateType,
            string productAction,
            List<string> validationErrorMessages)
        {
            var isValid = ValidateUserRoles(taskRole, validationErrorMessages);

            if (string.IsNullOrEmpty(taskName))
            {
                validationErrorMessages.Add("Task Information: Task Name cannot be empty");
                isValid = false;
            }

            if (string.IsNullOrEmpty(chartingArea))
            {
                validationErrorMessages.Add("Task Information: Charting Area cannot be empty");
                isValid = false;
            }
            if (string.IsNullOrEmpty(updateType))
            {
                validationErrorMessages.Add("Task Information: Update Type cannot be empty");
                isValid = false;
            }
            if (string.IsNullOrEmpty(productAction))
            {
                validationErrorMessages.Add("Task Information: Product Action Required cannot be empty");
                isValid = false;
            }

            return isValid;
        }

        private bool ValidateUserRoles(TaskRole taskRole, List<string> validationErrorMessages)
        {
            var isValid = true;


            var userList = _dbUpdateUserDbService.GetUsersFromDbAsync().Result.ToList();

            if (taskRole.Compiler == null)
            {
                validationErrorMessages.Add("Task Information: Compiler cannot be empty");
                isValid = false;
            }

            else

            {
                if (userList.All(a => a != taskRole.Compiler))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Compiler role to unknown user {taskRole.Compiler.DisplayName}");
                    isValid = false;
                }
            }


            if (taskRole.Verifier != null)
            {
                if (userList.All(a => a != taskRole.Verifier))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Verifier role to unknown user {taskRole.Verifier.DisplayName}");
                    isValid = false;
                }
            }

            return isValid;
        }

        public bool ValidateForCompletion(string assignedUser, string username, DbUpdateTaskStageType stageType,
            TaskRole role, DateTime? targetDate,
            List<string> validationErrorMessages)
        {
            var isValid = true;



            if (string.IsNullOrEmpty(assignedUser))
            {
                validationErrorMessages.Add("Please assign a user to this stage and Save before completion");
                isValid = false;

            }
            else
            {
                if ((assignedUser != username))
                {
                    validationErrorMessages.Add("Current user is not valid for completion of this task stage");
                    isValid = false;
                }
            }

            if (stageType == DbUpdateTaskStageType.Compile && (role.Verifier == null))
            {
                validationErrorMessages.Add("Please assign a user to Verifier role and Save before completing this stage");
                isValid = false;
            }

            return isValid;
        }

        public bool ValidateForRework(string assignedUser, string username,
            List<string> validationErrorMessages)
        {
            var isValid = true;


            if (string.IsNullOrEmpty(assignedUser))
            {
                validationErrorMessages.Add("Please assign a user to this stage before sending this task for Rework");
                isValid = false;

            }
            else
            {
                if ((assignedUser != username))
                {
                    validationErrorMessages.Add("Current user is not valid for sending this task for Rework");
                    isValid = false;
                }
            }


            return isValid;
        }

        public bool ValidateForCompleteWorkflow(string assignedUser, string username,
            List<string> validationErrorMessages)
        {
            var isValid = true;


            if (string.IsNullOrEmpty(assignedUser))
            {
                validationErrorMessages.Add("Please assign a user to the Verifier role and Save before completing the workflow");
                isValid = false;

            }
            else
            {
                if ((assignedUser != username))
                {
                    validationErrorMessages.Add("Only users assigned to the Verifier role are allowed to complete the workflow.");
                    isValid = false;
                }
            }


            return isValid;
        }

        public bool ValidateWorkflowPage(TaskRole taskRole, string productAction, DateTime? targetDate, List<string> validationErrorMessages)
        {

            var isValid = ValidateUserRoles(taskRole, validationErrorMessages);

            if (string.IsNullOrEmpty(productAction))
            {
                validationErrorMessages.Add("Task Information: Product Action Required cannot be empty");
                isValid = false;
            }

            return isValid;
        }
    }
}
