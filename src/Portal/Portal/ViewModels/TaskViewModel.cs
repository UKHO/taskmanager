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
        public short? DaysToDmEndDate { get; set; }
        public bool DaysToDmEndDateGreenAlert { get; set; }
        public bool DaysToDmEndDateAmberAlert { get; set; }
        public bool DaysToDmEndDateRedAlert { get; set; }
        public bool IsOnHold { get; set; }
        public bool OnHoldDaysAmber { get; set; }
        public bool OnHoldDaysGreen { get; set; }
        public bool OnHoldDaysRed { get; set; }
        public int OnHoldDays { get; set; }
        [DisplayFormat(NullDisplayText = "N/A")]
        public string AssessmentDataParsedRsdraNumber { get; set; }
        [DisplayFormat(NullDisplayText = "N/A")]
        public string AssessmentDataSourceDocumentName { get; set; }
        public string Workspace { get; set; }
        public string TaskStage { get; set; }
        public AdUser Reviewer { get; set; }
        public AdUser Assessor { get; set; }
        public AdUser Verifier { get; set; }
        public string Team { get; set; }
        public string Complexity { get; set; }
        public string TaskNoteText { get; set; }
        public List<Comment> Comment { get; set; }
    }
}
