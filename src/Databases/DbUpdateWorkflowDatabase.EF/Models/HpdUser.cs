namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class HpdUser
    {
        public int HpdUserId { get; set; }

        public string HpdUsername { get; set; }

        public virtual AdUser AdUser { get; set; }
    }
}
