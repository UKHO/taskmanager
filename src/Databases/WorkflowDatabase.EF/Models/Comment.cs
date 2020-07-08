using System;

namespace WorkflowDatabase.EF.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int ProcessId { get; set; }
        public string Text { get; set; }
        public int WorkflowInstanceId { get; set; }
        public DateTime Created { get; set; }

        public virtual AdUser AdUser { get; set; }
        public int AdUserId { get; set; }
    }
}
