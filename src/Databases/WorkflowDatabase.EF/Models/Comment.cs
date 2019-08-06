namespace WorkflowDatabase.EF.Models
{
    public class Comment
    {
        public int CommentId { get; set; }
        public int WorkflowProcessId { get; set; }
        public string Text { get; set; }
    }
}
