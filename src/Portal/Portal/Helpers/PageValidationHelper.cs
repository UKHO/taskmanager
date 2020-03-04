using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using Portal.Auth;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class PageValidationHelper : IPageValidationHelper
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly HpdDbContext _hpdDbContext;
        private readonly IUserIdentityService _userIdentityService;

        public PageValidationHelper(WorkflowDbContext dbContext, HpdDbContext hpdDbContext, IUserIdentityService userIdentityService)
        {
            _dbContext = dbContext;
            _hpdDbContext = hpdDbContext;
            _userIdentityService = userIdentityService;
        }

        /// <summary>
        /// Used in Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="reviewer"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        public async Task<bool> ValidateReviewPage(
            DbAssessmentReviewData primaryAssignedTask,
            List<DbAssessmentAssignTask> additionalAssignedTasks,
            List<string> validationErrorMessages,
            string reviewer, string team)
        {
            var isValid = true;

            if (!ValidateTaskType(primaryAssignedTask, additionalAssignedTasks, validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateOperators(reviewer, "Reviewer", validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateWorkspace(primaryAssignedTask, additionalAssignedTasks, validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateUsers(primaryAssignedTask, additionalAssignedTasks, validationErrorMessages))
            {
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(team))
            {
                validationErrorMessages.Add("Task Information: Team cannot be empty");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Used in Assess page
        /// </summary>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="taskType"></param>
        /// <param name="assessor"></param>
        /// <param name="verifier"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        public async Task<bool> ValidateAssessPage(
            string ion,
            string activityCode,
            string sourceCategory,
            string taskType,
            string assessor,
            string verifier,
            List<ProductAction> recordProductAction,
            List<DataImpact> dataImpacts,
            List<string> validationErrorMessages, string team)
        {
            var isValid = true;

            if (!ValidateAssessTaskInformation(ion, activityCode, sourceCategory, taskType, validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateOperators(assessor,"Assessor", validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateOperators(verifier,"Verifier", validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateRecordProductAction(recordProductAction, validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateDataImpact(dataImpacts, validationErrorMessages))
            {
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(team))
            {
                validationErrorMessages.Add("Task Information: Team cannot be empty");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Used in Verify page
        /// </summary>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="verifier"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="action"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        public async Task<bool> ValidateVerifyPage(string ion,
            string activityCode,
            string sourceCategory,
            string verifier,
            List<ProductAction> recordProductAction,
            List<DataImpact> dataImpacts,
            string action,
            List<string> validationErrorMessages,
            string team)
        {
            var isValid = true;

            if (!ValidateVerifyTaskInformation(ion, activityCode, sourceCategory, validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateOperators(verifier,"Verifier", validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateRecordProductAction(recordProductAction, action, validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateDataImpact(dataImpacts, action, validationErrorMessages))
            {
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(team))
            {
                validationErrorMessages.Add("Task Information: Team cannot be empty");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Used in the Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private bool ValidateTaskType(
                                        DbAssessmentReviewData primaryAssignedTask,
                                        List<DbAssessmentAssignTask> additionalAssignedTasks,
                                        List<string> validationErrorMessages)
        {
            if (string.IsNullOrEmpty(primaryAssignedTask.TaskType))
            {
                validationErrorMessages.Add($"Assign Task 1: Task Type is required");
                return false;
            }

            if (!_dbContext.AssignedTaskType.Any(st => st.Name == primaryAssignedTask.TaskType))
            {
                validationErrorMessages.Add($"Assign Task 1: Task Type {primaryAssignedTask.TaskType} does not exist");
                return false;
            }

            var taskTypes = additionalAssignedTasks.Select(st => st.TaskType).ToList();

            if (taskTypes.Any(s => string.IsNullOrEmpty(s)))
            {
                validationErrorMessages.Add($"Additional Assign Task: Task Type is required");
                return false;
            }

            var erroneousEntries = taskTypes.Except(_dbContext.AssignedTaskType.Select(st => st.Name));
            if (erroneousEntries.Any())
            {
                var entry = string.Join(',', erroneousEntries);
                validationErrorMessages.Add($"Additional Assign Task: Invalid Task Type - {entry}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Used in Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private bool ValidateWorkspace(
                                        DbAssessmentReviewData primaryAssignedTask,
                                        List<DbAssessmentAssignTask> additionalAssignedTasks,
                                        List<string> validationErrorMessages)
        {
            if (string.IsNullOrEmpty(primaryAssignedTask.WorkspaceAffected))
            {
                validationErrorMessages.Add($"Assign Task 1: Workspace Affected is required");
                return false;
            }

            var workspaceAffected = additionalAssignedTasks.Select(st => st.WorkspaceAffected).ToList();

            if (workspaceAffected.Any(s => string.IsNullOrEmpty(s)))
            {
                validationErrorMessages.Add($"Additional Assign Task: Workspace Affected is required");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Used in Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private bool ValidateUsers(
                                    DbAssessmentReviewData primaryAssignedTask,
                                    List<DbAssessmentAssignTask> additionalAssignedTasks,
                                    List<string> validationErrorMessages)
        {
            if (string.IsNullOrEmpty(primaryAssignedTask.Assessor))
            {
                validationErrorMessages.Add($"Assign Task 1: Assessor is required");
                return false;
            }

            var assessor = additionalAssignedTasks.Select(st => st.Assessor).ToList();

            if (assessor.Any(s => string.IsNullOrEmpty(s)))
            {
                validationErrorMessages.Add($"Additional Assign Task: Assessor is required");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Used in Assess pages
        /// </summary>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private bool ValidateAssessTaskInformation(string ion, string activityCode, string sourceCategory, string taskType, List<string> validationErrorMessages)
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(ion))
            {
                validationErrorMessages.Add("Task Information: Ion cannot be empty");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(activityCode))
            {
                validationErrorMessages.Add("Task Information: Activity code cannot be empty");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(sourceCategory))
            {
                validationErrorMessages.Add("Task Information: Source category cannot be empty");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(taskType))
            {
                validationErrorMessages.Add("Task Information: Task type cannot be empty");
                isValid = false;
            }

            return isValid;
        }


        /// <summary>
        /// Used in Verify pages
        /// </summary>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private bool ValidateVerifyTaskInformation(string ion, string activityCode, string sourceCategory, List<string> validationErrorMessages)
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(ion))
            {
                validationErrorMessages.Add("Task Information: Ion cannot be empty");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(activityCode))
            {
                validationErrorMessages.Add("Task Information: Activity code cannot be empty");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(sourceCategory))
            {
                validationErrorMessages.Add("Task Information: Source category cannot be empty");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="operatorUsername"></param>
        /// <param name="userTypeInMessage"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private async Task<bool> ValidateOperators(string operatorUsername, string userTypeInMessage, List<string> validationErrorMessages)
        {
            if (string.IsNullOrWhiteSpace(operatorUsername))
            {
                validationErrorMessages.Add($"Operators: {userTypeInMessage} cannot be empty");
                return false;
            }

            if (!await _userIdentityService.ValidateUser(operatorUsername))
            {
                validationErrorMessages.Add($"Operators: Unable to set {userTypeInMessage} to unknown user {operatorUsername}");
                return false;
            }
            return true;
        }


        /// <summary>
        /// Used in Verify pages
        /// </summary>
        /// <param name="recordProductAction"></param>
        /// <param name="action"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private async Task<bool> ValidateRecordProductAction(List<ProductAction> recordProductAction, string action, List<string> validationErrorMessages)
        {
            var isValid = await ValidateRecordProductAction(recordProductAction, validationErrorMessages);

            if (action != "Done")
            {
                return isValid;
            }

            if (recordProductAction != null && recordProductAction.Count > 0)
            {
                if (!recordProductAction.All(pa => pa.Verified))
                {
                    validationErrorMessages.Add(
                        $"Record Product Action: All Product Actions must be verified");
                    isValid = false;
                }


            }

            return isValid;
        }

        private async Task<bool> ValidateRecordProductAction(List<ProductAction> recordProductAction, List<string> validationErrorMessages)
        {
            bool isValid = true;

            if (recordProductAction != null && recordProductAction.Count > 0)
            {
                // Check at least one entry populated
                if (recordProductAction.Any(r =>
                    string.IsNullOrWhiteSpace(r.ImpactedProduct)
                    || r.ProductActionTypeId == 0))
                {
                    validationErrorMessages.Add($"Record Product Action: Please ensure impacted product is fully populated");
                    return false;
                }

                foreach (var productAction in recordProductAction)
                {
                    // Check for existing impacted products
                    var isExist = await _hpdDbContext.CarisProducts.AnyAsync(p =>
                        p.ProductStatus.Equals("Active", StringComparison.InvariantCultureIgnoreCase) &&
                        p.TypeKey.Equals("ENC", StringComparison.InvariantCultureIgnoreCase) &&
                        p.ProductName.Equals(productAction.ImpactedProduct, StringComparison.InvariantCultureIgnoreCase));

                    if (!isExist)
                    {
                        validationErrorMessages.Add(
                            $"Record Product Action: Impacted product {productAction.ImpactedProduct} does not exist");
                        isValid = false;
                    }
                }

                if (recordProductAction.GroupBy(p => p.ImpactedProduct)
                    .Where(g => g.Count() > 1)
                    .Select(y => y.Key).Any())
                {
                    validationErrorMessages.Add("Record Product Action: More than one of the same Impacted Products selected");
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Used in Verify pages
        /// </summary>
        /// <param name="dataImpacts"></param>
        /// <param name="action"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private bool ValidateDataImpact(List<DataImpact> dataImpacts, string action, List<string> validationErrorMessages)
        {
            var isValid = ValidateDataImpact(dataImpacts, validationErrorMessages);

            if (action != "Done")
            {
                return isValid;
            }

            if (dataImpacts != null && dataImpacts.Count > 0)
            {
                if (!dataImpacts.All(di => di.Verified))
                {
                    validationErrorMessages.Add(
                        $"Data Impact: All Usages must be verified");
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="dataImpacts"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private bool ValidateDataImpact(List<DataImpact> dataImpacts, List<string> validationErrorMessages)
        {
            if (dataImpacts == null || dataImpacts.Count == 0) return true;

            if (dataImpacts.All(d => d.HpdUsageId == 0))
            {
                return true;
            }

            // Show error to user that they've chosen the same usage more than once
            if (dataImpacts.GroupBy(x => x.HpdUsageId)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key).Any())
            {
                validationErrorMessages.Add("Data Impact: More than one of the same Usage selected");
                return false;
            }

            return true;
        }


    }
}
