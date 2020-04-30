using System;
using WorkflowDatabase.EF;

namespace Portal.Models
{
    public class HistoricalTasksData
    {
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }
        public DateTime DmEndDate { get; set; }
        public string RsdraNumber { get; set; }
        public string ParsedRsdraNumber => this.RsdraNumber.Replace("RSDRA", "");
        public string SourceDocumentName { get; set; }
        public WorkflowStage TaskStage { get; set; }
        public WorkflowStatus Status { get; set; }
        public string Reviewer { get; set; }
        public string Assessor { get; set; }
        public string Verifier { get; set; }
        public string Team { get; set; }
        public DateTime ActivityChangedAt { get; set; }
    }
}
