using System;

namespace WorkflowDatabase.EF.Models
{
    public class OnHold
    {
        public int OnHoldId { get; set; }
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }
        public DateTime OnHoldTime { get; set; }
        public DateTime? OffHoldTime { get; set; }
        public string OnHoldUser { get; set; }
        public string OffHoldUser { get; set; }
    }
}
