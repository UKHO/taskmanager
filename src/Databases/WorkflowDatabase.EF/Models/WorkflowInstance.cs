using System;
using System.Collections.Generic;

namespace WorkflowDatabase.EF.Models
{
    public class WorkflowInstance
    {
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }
        public string SerialNumber { get; set; }
        public int? ParentProcessId { get; set; }
        public string WorkflowType { get; set; }
        public string ActivityName { get; set; }
        public DateTime StartedAt { get; set; }
        public string Status { get; set; }
        public string AssignedTo { get; set; }

        public virtual List<Comment> Comments { get; set; }
        public virtual AssessmentData AssessmentData { get; set; }
        public virtual DbAssessmentReviewData DbAssessmentReviewData { get; set; }
        public virtual DbAssessmentAssessData DbAssessmentAssessData { get; set; }
        public virtual PrimaryDocumentStatus PrimaryDocumentStatus { get; set; }
        public virtual List<DatabaseDocumentStatus> DatabaseDocumentStatus { get; set; }
        public virtual List<LinkedDocument> LinkedDocument { get; set; }
        public virtual List<OnHold> OnHold { get; set; }
        public virtual TaskNote TaskNote { get; set; }
        public virtual List<DataImpact> DataImpact { get; set; }
        public virtual List<ProductAction> ProductAction { get; set; }
        public virtual List<DbAssessmentAssignTask> DbAssessmentAssignTask { get; set; }
    }
}
