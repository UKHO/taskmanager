using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal.ViewModels
{
    public class TaskViewModel
    {
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }
        public DateTime DmEndDate { get; set; }
        public short DaysToDmEndDate { get; set; }
        public bool DaysToDmEndDateAmberAlert { get; set; }
        public bool DaysToDmEndDateRedAlert { get; set; }
        public short DaysOnHold { get; set; }
        public string AssessmentDataRsdraNumber { get; set; }
        public string AssessmentDataSourceDocumentName { get; set; }
        public string Workspace { get; set; }
        public string TaskStage { get; set; }
        public string Reviewer { get; set; }
        public string Assessor { get; set; }
        public string Verifier { get; set; }
        public string Team { get; set; }
        public string TaskNoteText { get; set; }
        public List<Comment> Comment { get; set; }
    }
}
