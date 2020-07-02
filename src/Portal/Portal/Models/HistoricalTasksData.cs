using System;
using System.ComponentModel.DataAnnotations;
using WorkflowDatabase.EF;
using WorkflowDatabase.EF.Models;

namespace Portal.Models
{
    public class HistoricalTasksData
    {
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }

        [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:d}")]
        public DateTime? DmEndDate { get; set; }
        public string RsdraNumber { get; set; }
        public string ParsedRsdraNumber => this.RsdraNumber.Replace("RSDRA", "");
        public string SourceDocumentName { get; set; }
        public WorkflowStage TaskStage { get; set; }
        public WorkflowStatus Status { get; set; }
        public AdUser Reviewer { get; set; }
        public AdUser Assessor { get; set; }
        public AdUser Verifier { get; set; }
        public string Team { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime ActivityChangedAt { get; set; }
    }
}
