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

        public virtual List<Comments> Comment { get; set; }
        public virtual AssessmentData AssessmentData { get; set; }
        public virtual DbAssessmentReviewData DbAssessmentReviewData { get; set; }
        public virtual SourceDocumentStatus SourceDocumentStatus { get; set; }
    }
}
