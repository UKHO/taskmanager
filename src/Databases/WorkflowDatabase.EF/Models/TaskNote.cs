using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("TaskNote")]
    public class TaskNote
    {
        public int TaskNoteId { get; set; }
        public int ProcessId { get; set; }
        public string Text { get; set; }
        public int WorkflowInstanceId { get; set; }
        public string Username { get; set; }
        public DateTime Created { get; set; }
        public string CreatedByUsername { get; set; }
        public DateTime LastModified { get; set; }
        public string LastModifiedByUsername { get; set; }

    }
}
