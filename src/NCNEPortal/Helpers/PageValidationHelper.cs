using NCNEPortal.Auth;
using NCNEPortal.Enums;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCNEPortal.Helpers
{
    public class PageValidationHelper : IPageValidationHelper
    {
        private readonly NcneWorkflowDbContext _dbContext;
        private readonly IUserIdentityService _userIdentityService;
        private readonly IDirectoryService _directoryService;

        public PageValidationHelper(NcneWorkflowDbContext dbContext,
            IUserIdentityService userIdentityService,
            IDirectoryService directoryService
                        )
        {
            this._dbContext = dbContext;
            _userIdentityService = userIdentityService;
            _directoryService = directoryService;
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

        private bool ValidateThreePs(DateTime? sendDate3Ps, DateTime? expectedReturnDate3Ps, DateTime? actualReturnDate3Ps, List<string> validationErrorMessages)
        {
            bool isValid = true;

            if ((sendDate3Ps == null) && (expectedReturnDate3Ps != null || actualReturnDate3Ps != null))
            {
                validationErrorMessages.Add("3PS : Please enter date sent to 3PS before entering actual and expected return dates");
                isValid = false;
            }

            if ((expectedReturnDate3Ps == null) && (actualReturnDate3Ps != null))
            {
                validationErrorMessages.Add("3PS : Please enter expected return date before entering actual return date");
                isValid = false;
            }

            if (sendDate3Ps != null)
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

            var userList = _directoryService.GetGroupMembers().Result.ToList();

            if (string.IsNullOrEmpty(taskRole.Compiler))
            {
                validationErrorMessages.Add("Task Information: Compiler can not be empty");
                isValid = false;
            }

            else

            {
                if (!userList.Any(a => a == taskRole.Compiler))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Compiler role to unknown user {taskRole.Compiler}");
                    isValid = false;
                }
            }


            if (!string.IsNullOrEmpty(taskRole.VerifierOne))
            {
                if (!userList.Any(a => a == taskRole.VerifierOne))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Verifier1 role to unknown user {taskRole.VerifierOne}");
                    isValid = false;
                }
            }

            if (!string.IsNullOrEmpty(taskRole.VerifierTwo))
            {
                if (!userList.Any(a => a == taskRole.VerifierTwo))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Verifier2 role to unknown user {taskRole.VerifierTwo}");
                    isValid = false;
                }
            }

            if (!string.IsNullOrEmpty(taskRole.Publisher))
            {
                if (!userList.Any(a => a == taskRole.Publisher))
                {
                    validationErrorMessages.Add($"Task Information: Unable to assign Publisher role to unknown user {taskRole.Publisher}");
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}