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

        public virtual AdUser OnHoldBy { get; set; }
        public virtual AdUser OffHoldBy { get; set; }
    }
}
