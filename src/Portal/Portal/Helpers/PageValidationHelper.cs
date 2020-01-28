using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HpdDatabase.EF.Models;
using Microsoft.EntityFrameworkCore;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class PageValidationHelper : IPageValidationHelper
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly HpdDbContext _hpdDbContext;

        public PageValidationHelper(WorkflowDbContext dbContext, HpdDbContext hpdDbContext)
        {
            _dbContext = dbContext;
            _hpdDbContext = hpdDbContext;
        }

        /// <summary>
        /// Used in Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        public bool ValidatePage(
            DbAssessmentReviewData primaryAssignedTask,
            List<DbAssessmentAssignTask> additionalAssignedTasks,
            List<string> validationErrorMessages)
        {
            var isValid = true;

            if (!ValidateTaskType(primaryAssignedTask, additionalAssignedTasks, validationErrorMessages))
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

            return isValid;
        }

        /// <summary>
        /// Used in Assess and Verify page
        /// </summary>
        /// <param name="taskStage"></param>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="verifier"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        public async Task<bool> ValidatePage(
                                                string taskStage,
                                                string ion,
                                                string activityCode,
                                                string sourceCategory,
                                                string verifier,
                                                List<ProductAction> recordProductAction,
                                                List<DataImpact> dataImpacts,
                                                List<string> validationErrorMessages)
        {
            var isValid = true;

            if (!ValidateTaskInformation(ion, activityCode, sourceCategory, validationErrorMessages))
            {
                isValid = false;
            }

            if (!ValidateOperators(verifier, validationErrorMessages))
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

            return isValid;
        }

        /// <summary>
        /// Used in the Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        public bool ValidateTaskType(
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
        public bool ValidateWorkspace(
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
        public bool ValidateUsers(
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
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        public bool ValidateTaskInformation(string ion, string activityCode, string sourceCategory, List<string> validationErrorMessages)
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
        /// <param name="verifier"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        public bool ValidateOperators(string verifier, List<string> validationErrorMessages)
        {
            if (string.IsNullOrWhiteSpace(verifier))
            {
                validationErrorMessages.Add("Operators: Verifier cannot be empty");
                return false;
            }
            return true;
        }


        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="recordProductAction"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        public async Task<bool> ValidateRecordProductAction(List<ProductAction> recordProductAction, List<string> validationErrorMessages)
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
                        validationErrorMessages.Add($"Record Product Action: Impacted product {productAction.ImpactedProduct} does not exist");
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
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="dataImpacts"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        public bool ValidateDataImpact(List<DataImpact> dataImpacts, List<string> validationErrorMessages)
        {
            if (dataImpacts == null || dataImpacts.Count == 0) return true;

            if (dataImpacts.Any(d => d.HpdUsageId == 0))
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
