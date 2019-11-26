using System;

namespace WorkflowDatabase.EF.Models
{
    public class DataImpact
    {
        public int DataImpactId { get; set; }
        public int ProcessId { get; set; }
        public int HpdUsageId { get; set; }
        public bool Edited { get; set; }
        public string Comments { get; set; }
        public bool Verified { get; set; }

        public virtual HpdUsages HpdUsages { get; set; }
    }
}
