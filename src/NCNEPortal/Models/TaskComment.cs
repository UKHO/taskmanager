using System;

namespace NCNEPortal.Models
{
    public class TaskComment
    {
        public String Name { get; set; }
        public DateTime CommentDate { get; set; }
        public string CommentText { get; set; }

        public string Role { get; set; }
    }
}