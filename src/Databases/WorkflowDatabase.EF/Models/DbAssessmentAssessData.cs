using WorkflowDatabase.EF.Interfaces;

namespace WorkflowDatabase.EF.Models
{
    public class DbAssessmentAssessData : ITaskData
    {
        public int DbAssessmentAssessDataId { get; set; }
        public int ProcessId { get; set; }
        public string Ion { get; set; }
        public string ActivityCode { get; set; }
        public string SourceCategory { get; set; }
        public string Reviewer { get; set; }
        public string Assessor { get; set; }
        public string Verifier { get; set; }
        public string TaskType { get; set; }
        public int WorkflowInstanceId { get; set; }
        public bool ProductActioned { get; set; }
        public string ProductActionChangeDetails { get; set; }
    }
}