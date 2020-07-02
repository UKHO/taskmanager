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
        public AdUser OnHoldBy { get; set; }
        public AdUser OffHoldBy { get; set; }
    }
}
