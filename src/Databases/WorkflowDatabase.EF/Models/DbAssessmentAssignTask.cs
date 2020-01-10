using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class DbAssessmentAssignTask
    {
        public int DbAssessmentAssignTaskId { get; set; }
        public int ProcessId { get; set; }
        public string Assessor { get; set; }
        public string Verifier { get; set; }
        [DisplayName("Source Type:")]
        public string TaskType { get; set; }
        [DisplayName("Workspace Affected:")]
        public string WorkspaceAffected { get; set; }
        public string Notes { get; set; }
    }
}
