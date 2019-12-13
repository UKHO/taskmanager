using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("DbAssessmentReviewData")]
    public class DbAssessmentReviewData
    {
        public int DbAssessmentReviewDataId { get; set; }
        public int ProcessId { get; set; }
        public string Ion { get; set; }
        public string ActivityCode { get; set; }
        public string Assessor { get; set; }
        public string Verifier { get; set; }
        [DisplayName("Source Type:")]
        public string AssignedTaskSourceType { get; set; }
        [DisplayName("Workspace Affected:")]
        public string WorkspaceAffected { get; set; }
        public string Notes { get; set; }
        public string TaskComplexity { get; set; }
        public int WorkflowInstanceId { get; set; }
    }
}
