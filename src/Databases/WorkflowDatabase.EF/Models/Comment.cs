namespace WorkflowDatabase.EF.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int ProcessId { get; set; }

        public string Text { get; set; }
    }
}
