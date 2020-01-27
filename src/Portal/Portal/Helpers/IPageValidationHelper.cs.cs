using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public interface IPageValidationHelper
    {
        /// <summary>
        /// Used in Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        bool ValidatePage(
            DbAssessmentReviewData primaryAssignedTask,
            List<DbAssessmentAssignTask> additionalAssignedTasks,
            List<string> validationErrorMessages);

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
        Task<bool> ValidatePage(
            string taskStage,
            string ion, 
            string activityCode, 
            string sourceCategory,
            string verifier,
            List<ProductAction> recordProductAction, 
            List<DataImpact> dataImpacts, 
            List<string> validationErrorMessages);

        /// <summary>
        /// Used in the Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        bool ValidateTaskType(
            DbAssessmentReviewData primaryAssignedTask, 
            List<DbAssessmentAssignTask> additionalAssignedTasks, 
            List<string> validationErrorMessages);

        /// <summary>
        /// Used in Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        bool ValidateWorkspace(
            DbAssessmentReviewData primaryAssignedTask,
            List<DbAssessmentAssignTask> additionalAssignedTasks,
            List<string> validationErrorMessages);

        /// <summary>
        /// Used in Review page
        /// </summary>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        bool ValidateUsers(
            DbAssessmentReviewData primaryAssignedTask,
            List<DbAssessmentAssignTask> additionalAssignedTasks,
            List<string> validationErrorMessages);

        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        bool ValidateTaskInformation(string ion, string activityCode, string sourceCategory, List<string> validationErrorMessages);

        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="verifier"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        bool ValidateOperators(string verifier, List<string> validationErrorMessages);

        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="recordProductAction"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        Task<bool> ValidateRecordProductAction(List<ProductAction> recordProductAction, List<string> validationErrorMessages);

        /// <summary>
        /// Used in Assess and Verify pages
        /// </summary>
        /// <param name="dataImpacts"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        bool ValidateDataImpact(List<DataImpact> dataImpacts, List<string> validationErrorMessages);
    }
}