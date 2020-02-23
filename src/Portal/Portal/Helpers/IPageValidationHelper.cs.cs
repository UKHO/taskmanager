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
        Task<bool> ValidateReviewPage(
            DbAssessmentReviewData primaryAssignedTask,
            List<DbAssessmentAssignTask> additionalAssignedTasks,
            List<string> validationErrorMessages,
            string Reviewer);

        /// <summary>
        /// Used in Assess page
        /// </summary>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="taskType"></param>
        /// <param name="verifier"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="validationErrorMessages"></param>
        /// <returns></returns>
        Task<bool> ValidateAssessPage(
            string ion,
            string activityCode,
            string sourceCategory,
            string taskType,
            string verifier,
            List<ProductAction> recordProductAction,
            List<DataImpact> dataImpacts,
            List<string> validationErrorMessages);


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
        /// <returns></returns>
        Task<bool> ValidateVerifyPage(string ion,
            string activityCode,
            string sourceCategory,
            string verifier,
            List<ProductAction> recordProductAction,
            List<DataImpact> dataImpacts,
            string action,
            List<string> validationErrorMessages);
    }
}