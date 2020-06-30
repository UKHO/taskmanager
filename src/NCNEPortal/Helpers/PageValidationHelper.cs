using NCNEPortal.Auth;
using NCNEPortal.Enums;
using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCNEPortal.Helpers
{
    public class PageValidationHelper : IPageValidationHelper
    {
        private readonly INcneUserDbService _ncneUserDbService;

        public PageValidationHelper(INcneUserDbService ncneUserDbService)
        {
            _ncneUserDbService = ncneUserDbService;
        }

        public bool ValidateNewTaskPage(TaskRole taskRole, string workflowType, string chartType, List<string> validationErrorMessages)
        {
            bool isValid = ValidateUserRoles(taskRole, validationErrorMessages);

            if (string.IsNullOrEmpty(chartType))
            {
                validationErrorMessages.Add("Task Information: Chart Type cannot be empty");
                isValid = false;
            }
            if (string.IsNullOrEmpty(workflowType))
            {
                validationErrorMessages.Add("Task Information: Workflow Type cannot be empty");
                isValid = false;
            }

            return isValid;
        }

        public bool ValidateWorkflowPage(TaskRole taskRole, DateTime? publicationDate, DateTime? repromatDate,
            int dating,
            string chartType,
            (bool SentTo3Ps, DateTime? SendDate3ps, DateTime? ExpectedReturnDate3ps, DateTime? ActualReturnDate3ps)
                threePsInfo, List<string> validationErrorMessages)
        {
            bool isValid = ValidateUserRoles(taskRole, validationErrorMessages);

            if (!ValidateDates(publicationDate, repromatDate, dating, chartType, validationErrorMessages))
            {
                isValid = false;
            }

            if (threePsInfo.SentTo3Ps == true)
            {
                if (!ValidateThreePs(threePsInfo.SendDate3ps, threePsInfo.ExpectedReturnDate3ps,
                    threePsInfo.ActualReturnDate3ps, validationErrorMessages))
                {
                    isValid = false;
                }
            }

            return isValid;

        }

        public bool ValidateForCompletion(string assignedUser, string username, NcneTaskStageType stageType,
            TaskRole role, List<string> validationErrorMessages)
        {
            bool isValid = true;

            if (stageType != NcneTaskStageType.Forms)
            {
                if (string.IsNullOrEmpty(assignedUser))
                {
                    validationErrorMessages.Add("Please assign a user to this stage before completion");
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

                if (stageType == NcneTaskStageType.Compile && string.IsNullOrEmpty(role.VerifierOne))
                {
                    validationErrorMessages.Add("Please assign a user to V1 role before completing this stage");
                    isValid = false;
                }

                if (stageType == NcneTaskStageType.Final_Updating && string.IsNullOrEmpty(role.HundredPercentCheck))
                {
                    validationErrorMessages.Add("Please assign a user to 100% Check role before completing this stage");
                    isValid = false;
                }

                if (stageType == NcneTaskStageType.Hundred_Percent_Check && string.IsNullOrEmpty(role.VerifierOne))
                {
                    validationErrorMessages.Add("Please assign a user to V1 role before completing this stage");
                    isValid = false;
                }
            }

            return isValid;
        }

        public bool ValidateForRework(string assignedUser, string username,
            List<string> validationErrorMessages)
        {
            bool isValid = true;


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
            bool isValid = true;


            if (string.IsNullOrEmpty(assignedUser))
            {
                validationErrorMessages.Add("Please assign a user to the V1 role before completing the workflow");
                isValid = false;

            }
            else
            {
                if ((assignedUser != username))
                {
                    validationErrorMessages.Add("Only users assigned to the V1 role are allowed to complete the workflow.");
                    isValid = false;
                }
            }


            return isValid;
        }

        private bool ValidateThreePs(DateTime? sendDate3Ps, DateTime? expectedReturnDate3Ps, DateTime? actualReturnDate3Ps, List<string> validationErrorMessages)
        {
            bool isValid = true;

            if (sendDate3Ps == null)
            {
                if (expectedReturnDate3Ps != null)
                {
                    validationErrorMessages.Add(
                        "3PS : Please enter date sent to 3PS before entering expected return date");
                    isValid = false;
                }

                if (actualReturnDate3Ps != null)
                {
                    validationErrorMessages.Add(
                        "3PS : Please enter date sent to 3PS before entering actual return date");
                    isValid = false;

                }
            }

            else

            {
                if ((expectedReturnDate3Ps != null) && (expectedReturnDate3Ps < sendDate3Ps))
                {
                    validationErrorMessages.Add(("3PS : Expected return date cannot be earlier than Sent to 3PS date"));
                    isValid = false;
                }

                if ((actualReturnDate3Ps != null) && (actualReturnDate3Ps < sendDate3Ps))
                {
                    validationErrorMessages.Add(("3PS : Actual return date cannot be earlier than Sent to 3PS date"));
                    isValid = false;
                }
            }

            if ((expectedReturnDate3Ps == null) && (actualReturnDate3Ps != null))
            {
                validationErrorMessages.Add("3PS : Please enter expected return date before entering actual return date");
                isValid = false;
            }

            return isValid;
        }

        private bool ValidateDates(DateTime? publicationDate, DateTime? repromatDate, int dating, string chartType, List<string> validationErrorMessages)
        {
            bool isValid = true;

            if (!Enum.IsDefined(typeof(DeadlineEnum), dating))
            {
                validationErrorMessages.Add("Task Information: Duration cannot be empty");
                isValid = false;

            }

            if (chartType == "Adoption")
            {
                if (repromatDate == null)
                {
                    validationErrorMessages.Add("Task Information: Repromat Date cannot be empty");
                    isValid = false;
                }
            }
            else
            {
                if (publicationDate == null)
                {
                    validationErrorMessages.Add("Task Information: Publication Date cannot be empty");
                    isValid = false;

                }
            }


            return isValid;

        }

        private bool ValidateUserRoles(TaskRole taskRole, List<string> validationErrorMessages)
        {
            bool isValid = true;

            var userList = _ncneUserDbService.GetUsersFromDbAsync().Result.Select(u => u.DisplayName).ToList();

            if (string.IsNullOrEmpty(taskRole.Compiler))
            {
                validationErrorMessages.Add("Task Information: Compiler cannot be empty");
                isValid = false;
            }

            else

            {
                if (userList.All(a => a != taskRole.Compiler))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Compiler role to unknown user {taskRole.Compiler}");
                    isValid = false;
                }
            }


            if (!string.IsNullOrEmpty(taskRole.VerifierOne))
            {
                if (userList.All(a => a != taskRole.VerifierOne))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Verifier1 role to unknown user {taskRole.VerifierOne}");
                    isValid = false;
                }
            }

            if (!string.IsNullOrEmpty(taskRole.VerifierTwo))
            {
                if (userList.All(a => a != taskRole.VerifierTwo))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Verifier2 role to unknown user {taskRole.VerifierTwo}");
                    isValid = false;
                }
            }

            if (!string.IsNullOrEmpty(taskRole.HundredPercentCheck))
            {
                if (userList.All(a => a != taskRole.HundredPercentCheck))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign 100% Check role to unknown user {taskRole.HundredPercentCheck}");
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}