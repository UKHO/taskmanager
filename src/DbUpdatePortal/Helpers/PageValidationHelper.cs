using DbUpdatePortal.Auth;
using DbUpdateWorkflowDatabase.EF.Models;
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

            if (string.IsNullOrEmpty(chartingArea))
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
                validationErrorMessages.Add("Task Information : Product Action Required cannot be empty");
                isValid = false;
            }

            return isValid;
        }

        private bool ValidateUserRoles(TaskRole taskRole, List<string> validationErrorMessages)
        {
            bool isValid = true;


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
    }
}
