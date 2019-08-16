using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowDatabase.EF.Models
{
    public class WorkflowInstance
    {
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }
        public string SerialNumber { get; set; }
        public int? ParentProcessId { get; set; }
        public string WorkflowType { get; set; }
        public string ActivityName { get; set; }
    }
}
