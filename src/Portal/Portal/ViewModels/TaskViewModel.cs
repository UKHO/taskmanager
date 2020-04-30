using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WorkflowDatabase.EF.Models;

namespace Portal.ViewModels
{
    public class TaskViewModel
    {
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }
        [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:d}")]
        public DateTime? DmEndDate { get; set; }
        [DisplayFormat(NullDisplayText = "N/A")]
        public short? DaysToDmEndDate { get; set; }
        public bool DaysToDmEndDateAmberAlert { get; set; }
        public bool DaysToDmEndDateRedAlert { get; set; }
        public bool IsOnHold { get; set; }
        public bool OnHoldDaysAmber { get; set; }
        public bool OnHoldDaysGreen { get; set; }
        public bool OnHoldDaysRed { get; set; }
        public int OnHoldDays { get; set; }
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
