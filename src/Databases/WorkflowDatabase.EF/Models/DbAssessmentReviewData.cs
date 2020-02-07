using System.ComponentModel;
using WorkflowDatabase.EF.Interfaces;

namespace WorkflowDatabase.EF.Models
{
    public class DbAssessmentReviewData : ITaskData
    {
        public int DbAssessmentReviewDataId { get; set; }
        public int ProcessId { get; set; }
        public string Ion { get; set; }
        public string ActivityCode { get; set; }
        public string SourceCategory { get; set; }
        public string Assessor { get; set; }
        public string Verifier { get; set; }
        [DisplayName("Task Type:")]
        public string TaskType { get; set; }
        [DisplayName("Workspace Affected:")]
        public string WorkspaceAffected { get; set; }
        [DisplayName("Caris Project Name:")]
        public string CarisProjectName { get; set; }
        public string Reviewer { get; set; }
        public string Notes { get; set; }
        public int WorkflowInstanceId { get; set; }
    }
}
