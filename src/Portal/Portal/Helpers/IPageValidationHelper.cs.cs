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
        /// <param name="action"></param>
        /// <param name="primaryAssignedTask"></param>
        /// <param name="additionalAssignedTasks"></param>
        /// <param name="team"></param>
        /// <param name="reviewer"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="currentUsername"></param>
        /// <param name="currentAssignedReviewerInDb"></param>
        /// <returns></returns>
        Task<bool> CheckReviewPageForErrors(
                                            string action,
                                            DbAssessmentReviewData primaryAssignedTask,
                                            List<DbAssessmentAssignTask> additionalAssignedTasks,
                                            string team,
                                            string reviewer,
                                            List<string> validationErrorMessages,
                                            string currentUsername,
                                            string currentAssignedReviewerInDb);

        /// <summary>
        /// Used in Assess page
        /// </summary>
        /// <param name="action"></param>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="taskType"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="team"></param>
        /// <param name="assessor"></param>
        /// <param name="verifier"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="currentUsername"></param>
        /// <param name="currentAssignedAssessorInDb"></param>
        /// <returns></returns>
        Task<bool> CheckAssessPageForErrors(
                                            string action,
                                            string ion,
                                            string activityCode,
                                            string sourceCategory,
                                            string taskType,
                                            List<ProductAction> recordProductAction,
                                            List<DataImpact> dataImpacts,
                                            string team,
                                            string assessor,
                                            string verifier,
                                            List<string> validationErrorMessages,
                                            string currentUsername,
                                            string currentAssignedAssessorInDb);
        /// <summary>
        /// Check for warnings
        /// </summary>
        /// <param name="action"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="validationWarningMessages"></param>
        /// <returns></returns>
        bool CheckAssessPageForWarnings(
            string action,
            List<DataImpact> dataImpacts,
            List<string> validationWarningMessages);


        /// <summary>
        /// Used in Verify page
        /// </summary>
        /// <param name="action"></param>
        /// <param name="ion"></param>
        /// <param name="activityCode"></param>
        /// <param name="sourceCategory"></param>
        /// <param name="formDataAssignedVerifier"></param>
        /// <param name="recordProductAction"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="team"></param>
        /// <param name="validationErrorMessages"></param>
        /// <param name="currentUsername"></param>
        /// <param name="currentAssignedVerifierInDb"></param>
        /// <returns></returns>
        Task<bool> CheckVerifyPageForErrors(
                                            string action,
                                            string ion,
                                            string activityCode,
                                            string sourceCategory,
                                            string formDataAssignedVerifier,
                                            List<ProductAction> recordProductAction,
                                            List<DataImpact> dataImpacts,
                                            string team,
                                            List<string> validationErrorMessages,
                                            string currentUsername,
                                            string currentAssignedVerifierInDb = "");

        /// <summary>
        /// Check for warnings
        /// </summary>
        /// <param name="action"></param>
        /// <param name="workflowInstance"></param>
        /// <param name="dataImpacts"></param>
        /// <param name="validationWarningMessages"></param>
        /// <returns></returns>
        Task<bool> CheckVerifyPageForWarnings(
                                                string action, 
                                                WorkflowInstance workflowInstance, 
                                                List<DataImpact> dataImpacts, 
                                                List<string> validationWarningMessages);

    }
}