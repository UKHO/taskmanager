using System.ComponentModel;
using WorkflowDatabase.EF.Interfaces;

namespace WorkflowDatabase.EF.Models
{
    public class DbAssessmentReviewData : ITaskData, IOperatorData
    {
        public int DbAssessmentReviewDataId { get; set; }
        public int ProcessId { get; set; }
        public string Ion { get; set; }
        public string ActivityCode { get; set; }
        public string SourceCategory { get; set; }
        [DisplayName("Assessor:")]
        public virtual AdUser Assessor { get; set; }
        public int? AssessorAdUserId { get; set; }
        [DisplayName("Verifier:")]
        public virtual AdUser Verifier { get; set; }
        public int? VerifierAdUserId { get; set; }
        [DisplayName("Task Type:")]
        public string TaskType { get; set; }
        [DisplayName("Workspace Affected:")]
        public string WorkspaceAffected { get; set; }
        public virtual AdUser Reviewer { get; set; }
        public int? ReviewerAdUserId { get; set; }

        [DisplayName("Notes:")]
        public string Notes { get; set; }
        public int WorkflowInstanceId { get; set; }
    }
}
