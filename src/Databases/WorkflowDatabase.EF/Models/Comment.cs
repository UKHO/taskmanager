using System;

namespace WorkflowDatabase.EF.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int ProcessId { get; set; }
        public string Text { get; set; }
        public int WorkflowInstanceId { get; set; }
        public string Username { get; set; }
        public DateTime Created { get; set; }
    }
}
