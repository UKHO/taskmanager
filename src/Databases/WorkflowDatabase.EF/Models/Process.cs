namespace WorkflowDatabase.EF.Models
{
    public class Process
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public string SerialNumber { get; set; }
        public int? ParentProcessId { get; set; }
        public string WorkflowType { get; set; }
        public string ActivityName { get; set; }
    }
}
