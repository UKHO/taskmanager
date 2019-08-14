using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("Process")]
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
