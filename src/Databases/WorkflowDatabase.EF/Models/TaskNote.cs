using System;

namespace WorkflowDatabase.EF.Models
{
    public class TaskNote
    {
        public int TaskNoteId { get; set; }
        public int ProcessId { get; set; }
        public string Text { get; set; }
        public int WorkflowInstanceId { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }

        public virtual AdUser CreatedBy { get; set; }
        public int? CreatedByAdUserId { get; set; }

        public virtual AdUser LastModifiedBy { get; set; }
        public int? LastModifiedByAdUserId { get; set; }

    }
}
