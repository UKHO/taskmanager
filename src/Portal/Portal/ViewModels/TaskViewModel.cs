using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal.ViewModels
{
    public class TaskViewModel
    {
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }
        public short DaysToDmEndDate { get; set; }
        public DateTime DmEndDate { get; set; }
        public short DaysOnHold { get; set; }
        public string AssessmentDataRsdraNumber { get; set; }
        public string AssessmentDataSourceDocumentName { get; set; }
        public string Workspace { get; set; }
        public string TaskType { get; set; }
        public string TaskStage { get; set; }
        public string DbAssessmentReviewDataAssessor { get; set; }
        public string DbAssessmentReviewDataVerifier { get; set; }
        public string Team { get; set; }
        public string TaskNote { get; set; }
        public List<Comments> Comment { get; set; }
    }
}
