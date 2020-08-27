using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Common.Helpers.Auth;
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
        private readonly IPortalUserDbService _portalAduserDbService;

        public PageValidationHelper(WorkflowDbContext dbContext, HpdDbContext hpdDbContext, IAdDirectoryService adDirectoryService, IPortalUserDbService portalAduserDbService)
        {
            _dbContext = dbContext;
            _hpdDbContext = hpdDbContext;
            _portalAduserDbService = portalAduserDbService;
        }

        /// <summary>
        /// Used in Review page
        /// </summary>
        /// <param name="action"></param>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="team"></param>
        /// <param name="reviewer"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="currentUsername"></param>
        /// <param name="currentAssignedReviewerInDb"></param>
        /// <returns></returns>
        public async Task<bool> CheckReviewPageForErrors(string action, DbAssessmentReviewData primaryAssignedTask,
            List<DbAssessmentAssignTask> additionalAssignedTasks,
            string team,
            AdUser reviewer,
            List<string> validationErrorMessages, string currentUserEmail,
            AdUser currentAssignedReviewerInDb)
        {
            var isValid = true;

            if (action == "Done")
            {
                if (!await ValidateOperators(reviewer, "Reviewer", validationErrorMessages))
                {
                    return false;
                }

                if (currentAssignedReviewerInDb.HasNoUserPrincipalName)
                {
                    validationErrorMessages.Add($"Operators: You are not assigned as the Reviewer of this task. Please assign the task to yourself and click Save");
                    isValid = false;
                }
                else if (!currentUserEmail.Equals(currentAssignedReviewerInDb.UserPrincipalName, StringComparison.InvariantCultureIgnoreCase))
                {
                    validationErrorMessages.Add($"Operators: {currentAssignedReviewerInDb.DisplayName} is assigned to this task. Please assign the task to yourself and click Save");
                    isValid = false;
                }

                if (!ValidateTaskType(primaryAssignedTask, additionalAssignedTasks, validationErrorMessages))
                {
                    isValid = false;
                }

                if (!ValidateWorkspace(primaryAssignedTask, additionalAssignedTasks, validationErrorMessages))
                {
                    isValid = false;
                }

                if (!await ValidateUsers(primaryAssignedTask, additionalAssignedTasks, validationErrorMessages))
                {
                    isValid = false;
                }
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
        /// <param name="action"></param>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="taskType"></param>
        /// <param name="productActioned"></param>
        /// <param name="productActionChangeDetails"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="stsDataImpact"></param>
        /// <param name="team"></param>
        /// <param name="assessor"></param>
        /// <param name="verifier"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="currentUsername"></param>
        /// <param name="currentAssignedAssessorInDb"></param>
        /// <returns></returns>
        public async Task<bool> CheckAssessPageForErrors(string action,
            string ion,
            string activityCode,
            string sourceCategory,
            string taskType,
            bool productActioned,
            string productActionChangeDetails,
            List<ProductAction> recordProductAction,
            List<DataImpact> dataImpacts,
            DataImpact stsDataImpact,
            string team,
            AdUser assessor,
            AdUser verifier,
            List<string> validationErrorMessages,
            string currentUserEmail,
            AdUser currentAssignedAssessorInDb)
        {
            var isValid = true;

            if (action == "Done")
            {
                if (currentAssignedAssessorInDb is null)
                {
                    validationErrorMessages.Add($"Operators: You are not assigned as the Assessor of this task. Please assign the task to yourself and click Save");
                    isValid = false;
                }
                else if (currentUserEmail != currentAssignedAssessorInDb.UserPrincipalName)
                {
                    validationErrorMessages.Add($"Operators: {currentAssignedAssessorInDb.DisplayName} is assigned to this task. Please assign the task to yourself and click Save");
                    isValid = false;
                }
            }

            if (!ValidateAssessTaskInformation(ion, activityCode, sourceCategory, taskType, validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateOperators(assessor, "Assessor", validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateOperators(verifier, "Verifier", validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateRecordProductAction(productActioned, productActionChangeDetails, recordProductAction, validationErrorMessages))
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
        /// Check for warnings
        /// </summary>
        /// <param name="action"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="stsDataImpact"></param>
        /// <param name="validationWarningMessages"></param>
        /// <returns></returns>
        public bool CheckAssessPageForWarnings(string action,
            List<DataImpact> dataImpacts,
            DataImpact stsDataImpact,
            List<string> validationWarningMessages)
        {
            var hasWarnings = false;

            if (!CheckDataImpactFeatures(action, WorkflowStage.Assess, dataImpacts, validationWarningMessages))
            {
                hasWarnings = true;
            }

            if (!CheckStsDataImpact(action, WorkflowStage.Assess, stsDataImpact, validationWarningMessages))
            {
                hasWarnings = true;
            }

            return hasWarnings;
        }

        /// <summary>
        /// Used in Verify page
        /// </summary>
        /// <param name="action"></param>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="formDataAssignedVerifier"></param>
        /// <param name="productActioned"></param>
        /// <param name="productActionChangeDetails"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="stsDataImpact"></param>
        /// <param name="team"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="currentUsername"></param>
        /// <param name="currentAssignedVerifierInDb"></param>
        /// <param name="isOnHold"></param>
        /// <returns></returns>
        public async Task<bool> CheckVerifyPageForErrors(string action,
            string ion,
            string activityCode,
            string sourceCategory,
            AdUser formDataAssignedVerifier,
            bool productActioned,
            string productActionChangeDetails,
            List<ProductAction> recordProductAction,
            List<DataImpact> dataImpacts,
            DataImpact stsDataImpact,
            string team,
            List<string> validationErrorMessages,
            string currentUserEmail,
            AdUser currentAssignedVerifierInDb = null,
            bool isOnHold = false)
        {
            var isValid = true;

            if (action == "Done")
            {
                if (currentAssignedVerifierInDb is null)
                {
                    validationErrorMessages.Add($"Operators: You are not assigned as the Verifier of this task. Please assign the task to yourself and click Save");
                    isValid = false;
                }
                else if (currentUserEmail != currentAssignedVerifierInDb.UserPrincipalName)
                {
                    validationErrorMessages.Add($"Operators: {currentAssignedVerifierInDb.DisplayName} is assigned to this task. Please assign the task to yourself and click Save");
                    isValid = false;
                }

                if (isOnHold)
                {
                    validationErrorMessages.Add("Task Information: Unable to Sign-off task.Take task off hold before signing-off and click Save.");
                    isValid = false;
                }
            }

            if (!ValidateVerifyTaskInformation(ion, activityCode, sourceCategory, validationErrorMessages))
            {
                isValid = false;
            }

            if (!await ValidateOperators(formDataAssignedVerifier, "Verifier", validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateRecordProductAction(productActioned, productActionChangeDetails, recordProductAction, action, validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateDataImpact(dataImpacts, validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateStsDataImpact(stsDataImpact, action, validationErrorMessages))
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
        /// Check for warnings
        /// </summary>
        /// <param name="action"></param>
        /// <param name="workflowInstance"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="stsDataImpact"></param>
        /// <param name="validationWarningMessages"></param>
        /// <returns></returns>
        public async Task<bool> CheckVerifyPageForWarnings(string action,
            WorkflowInstance workflowInstance,
            List<DataImpact> dataImpacts,
            DataImpact stsDataImpact,
            List<string> validationWarningMessages)
        {
            var hasWarnings = false;

            if (await HasActiveChildTasks(workflowInstance, validationWarningMessages))
            {
                hasWarnings = true;
            }

            if (!CheckDataImpactFeatures(action, WorkflowStage.Verify, dataImpacts, validationWarningMessages))
            {
                hasWarnings = true;
            }

            if (!CheckStsDataImpact(action, WorkflowStage.Verify, stsDataImpact, validationWarningMessages))
            {
                hasWarnings = true;
            }

            return hasWarnings;
        }

        private async Task<bool> HasActiveChildTasks(WorkflowInstance workflowInstance, List<string> validationWarningMessages)
        {
            if (workflowInstance.ParentProcessId == null)
            {
                var childProcessIds = await _dbContext.WorkflowInstance
                    .Where(wi => wi.ParentProcessId == workflowInstance.ProcessId)
                    .Where(wi =>
                        wi.Status == WorkflowStatus.Started.ToString() || wi.Status == WorkflowStatus.Updating.ToString())
                    .Select(wi => wi.ProcessId)
                    .ToListAsync();


                if (childProcessIds.Any())
                {
                    var joined = string.Join(',', childProcessIds);

                    validationWarningMessages.Add($"Child Tasks: The current task has the following active child tasks: {joined}.");

                    return true;
                }
            }

            return false;
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
        private async Task<bool> ValidateUsers(
                                    DbAssessmentReviewData primaryAssignedTask,
                                    List<DbAssessmentAssignTask> additionalAssignedTasks,
                                    List<string> validationErrorMessages)
        {
            if (primaryAssignedTask.Assessor.HasNoUserPrincipalName)
            {
                validationErrorMessages.Add($"Assign Task 1: Assessor is required");
                return false;
            }

            if (!await _portalAduserDbService.ValidateUserAsync(primaryAssignedTask.Assessor))
            {
                validationErrorMessages.Add($"Assign Task 1: Unable to set Assessor to unknown user {primaryAssignedTask.Assessor.DisplayName}");
                return false;
            }

            if (!string.IsNullOrEmpty(primaryAssignedTask.Verifier?.UserPrincipalName) &&
                !await _portalAduserDbService.ValidateUserAsync(primaryAssignedTask.Verifier))
            {
                validationErrorMessages.Add($"Assign Task 1: Unable to set Verifier to unknown user {primaryAssignedTask.Verifier.DisplayName}");
                return false;
            }

            var additionalAssignedTaskAssessors = additionalAssignedTasks.Select(st => st.Assessor).ToList();
            foreach (var assessor in additionalAssignedTaskAssessors)
            {
                if (string.IsNullOrEmpty(assessor?.UserPrincipalName))
                {
                    validationErrorMessages.Add($"Additional Assign Task: Assessor is required");
                    return false;
                }

                if (!await _portalAduserDbService.ValidateUserAsync(assessor))
                {
                    validationErrorMessages.Add($"Additional Assign Task: Unable to set Assessor to unknown user {assessor.DisplayName}");
                    return false;
                }
            }

            var additionalAssignedTaskVerifiers = additionalAssignedTasks.Select(st => st.Verifier).ToList();
            foreach (var verifier in additionalAssignedTaskVerifiers)
            {
                if (!string.IsNullOrEmpty(verifier?.UserPrincipalName) &&
                    !await _portalAduserDbService.ValidateUserAsync(verifier))
                {
                    validationErrorMessages.Add($"Additional Assign Task: Unable to set Verifier to unknown user {verifier.DisplayName}");
                    return false;
                }
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
        /// <param name="user"></param>
        /// <param name="userTypeInMessage"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private async Task<bool> ValidateOperators(AdUser user, string userTypeInMessage, List<string> validationErrorMessages)
        {
            if (user == null || user.HasNoUserPrincipalName)
            {
                validationErrorMessages.Add($"Operators: {userTypeInMessage} cannot be empty");
                return false;
            }

            if (!await _portalAduserDbService.ValidateUserAsync(user))
            {
                validationErrorMessages.Add($"Operators: Unable to set {userTypeInMessage} to unknown user {user.DisplayName}");
                return false;
            }
            return true;
        }


        /// <summary>
        /// Used in Verify pages
        /// </summary>
        /// <param name="productActionChangeDetails"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="action"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="productActioned"></param>
        /// <returns></returns>
        private bool ValidateRecordProductAction(bool productActioned, string productActionChangeDetails, List<ProductAction> recordProductAction, string action, List<string> validationErrorMessages)
        {
            var isValid = ValidateRecordProductAction(productActioned, productActionChangeDetails, recordProductAction, validationErrorMessages);

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

        private bool ValidateRecordProductAction(bool productActioned, string productActionChangeDetails, List<ProductAction> recordProductAction, List<string> validationErrorMessages)
        {
            const string messagePrefix = "Record Product Action:";

            var isValid = true;

            if (productActionChangeDetails?.Length > 250)
            {
                validationErrorMessages.Add($"{messagePrefix} Please ensure product action change details does not exceed 250 characters");
                return false;
            }

            if (!productActioned)
                return true;

            if (string.IsNullOrWhiteSpace(productActionChangeDetails))
            {
                validationErrorMessages.Add($"{messagePrefix} Please ensure you have entered product action change details");
                return false;
            }

            if (recordProductAction != null && recordProductAction.Count > 0)
            {
                // Check at least one entry populated
                if (recordProductAction.Any(r =>
                    string.IsNullOrWhiteSpace(r.ImpactedProduct)
                    || r.ProductActionTypeId == 0))
                {
                    validationErrorMessages.Add($"{messagePrefix} Please ensure impacted product is fully populated");
                    return false;
                }

                if (recordProductAction.GroupBy(p => p.ImpactedProduct)
                    .Where(g => g.Count() > 1)
                    .Select(y => y.Key).Any())
                {
                    validationErrorMessages.Add($"{messagePrefix} More than one of the same Impacted Products selected");
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="action"></param>
        /// <param name="workflowStage"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="validationWarningMessages"></param>
        /// <returns></returns>
        private bool CheckDataImpactFeatures(
                                                string action,
                                                WorkflowStage workflowStage,
                                                List<DataImpact> dataImpacts,
                                                List<string> validationWarningMessages)
        {

            if (action == "Save") return true;

            if (dataImpacts == null || dataImpacts.Count <= 0) return true;

            if (dataImpacts.All(di => di.HpdUsageId == 0)) return true;

            switch (workflowStage)
            {
                case WorkflowStage.Assess:
                    if (!dataImpacts.All(di => di.FeaturesSubmitted))
                    {
                        validationWarningMessages.Add(
                            "Data Impact: There are incomplete Features Submitted tick boxes.");
                        return false;
                    }

                    return true;
                case WorkflowStage.Verify:
                    if (!dataImpacts.All(di => di.FeaturesVerified))
                    {
                        validationWarningMessages.Add(
                            "Data Impact: There are incomplete Features Verified tick boxes.");
                        return false;
                    }

                    return true;
                default:
                    throw new NotImplementedException($"{workflowStage} not implemented");
            }
        }

        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="action"></param>
        /// <param name="workflowStage"></param>
        /// <param name="stsDataImpact"></param>
        /// <param name="validationWarningMessages"></param>
        /// <returns></returns>
        private bool CheckStsDataImpact(
                                                string action,
                                                WorkflowStage workflowStage,
                                                DataImpact stsDataImpact,
                                                List<string> validationWarningMessages)
        {
            if (action != "Done") return true;

            switch (workflowStage)
            {
                case WorkflowStage.Assess:
                case WorkflowStage.Verify:
                    if (stsDataImpact == null || stsDataImpact.HpdUsageId == 0)
                    {
                        validationWarningMessages.Add(
                            "Data Impact: STS Usage has not been selected, are you sure you want to continue?");
                        return false;
                    }

                    break;
                default:
                    throw new NotImplementedException($"{workflowStage} not implemented");
            }

            return true;
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

        /// <summary>
        /// Used in Verify page
        /// </summary>
        /// <param name="stsDataImpact"></param>
        /// <param name="action"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        private bool ValidateStsDataImpact(DataImpact stsDataImpact, string action,
            List<string> validationErrorMessages)
        {
            if (stsDataImpact == null || stsDataImpact.HpdUsageId == 0 ||
                action != "Done")
            {
                return true;
            }

            // Show error to user that they've chosen the same usage more than once
            if (!stsDataImpact.FeaturesVerified)
            {
                validationErrorMessages.Add("Data Impact: STS Usage has not been Verified");
                return false;
            }

            return true;
        }

    }
}
