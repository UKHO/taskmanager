using System;

namespace Database.SQL.EF.Models
{
    public class Task
    {
        public int Id { get; set; }
        public string ProcessId { get; set; }
        public int DaysToDmEndDate { get; set; }
        public DateTime DmEndDate { get; set; }
        public int DaysOnHold { get; set; }
        public string RsdraNo { get; set; }
        public string SourceName { get; set; }
        public string Workspace { get; set; }
        public string TaskType { get; set; }
        public string TaskStage { get; set; }
        public string Assessor { get; set; }
        public string Verifier { get; set; }
        public string Team { get; set; }
        public string TaskNote { get; set; }
    }
}
