using System.ComponentModel;
using WorkflowDatabase.EF.Interfaces;

namespace WorkflowDatabase.EF.Models
{
    public class DbAssessmentVerifyData : ITaskData, IProductActionData, IOperatorData
    {
        public int DbAssessmentVerifyDataId { get; set; }
        public int ProcessId { get; set; }
        public string Ion { get; set; }
        public string ActivityCode { get; set; }
        public string SourceCategory { get; set; }
        public virtual AdUser Reviewer { get; set; }
        public int? ReviewerAdUserId { get; set; }
        public virtual AdUser Assessor { get; set; }
        public int? AssessorAdUserId { get; set; }
        public virtual AdUser Verifier { get; set; }
        public int? VerifierAdUserId { get; set; }
        public string TaskType { get; set; }
        public int WorkflowInstanceId { get; set; }
        public bool ProductActioned { get; set; }
        public string ProductActionChangeDetails { get; set; }
        [DisplayName("Caris Workspace:")]
        public string WorkspaceAffected { get; set; }
    }
}