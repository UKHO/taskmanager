using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class DbAssessmentAssignTask
    {
        public int DbAssessmentAssignTaskId { get; set; }
        public int ProcessId { get; set; }
        [DisplayName("Assessor:")]
        public virtual AdUser Assessor { get; set; }
        [DisplayName("Verifier:")]
        public virtual AdUser Verifier { get; set; }
        [DisplayName("Task Type:")]
        public string TaskType { get; set; }
        [DisplayName("Workspace Affected:")]
        public string WorkspaceAffected { get; set; }
        [DisplayName("Notes:")]
        public string Notes { get; set; }
        public string Status { get; set; }
    }
}
