namespace WorkflowDatabase.EF.Models
{
    public class Process
    {
        public int ProcessId { get; set; }
        public int WorkflowProcessId { get; set; }
        public string SerialNumber { get; set; }
        public int? ParentWorkflowProcessId { get; set; }
        public string WorkflowType { get; set; }
        public string ActivityName { get; set; }
    }
}
