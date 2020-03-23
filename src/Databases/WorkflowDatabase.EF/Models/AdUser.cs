using System;

namespace WorkflowDatabase.EF.Models
{
    public class AdUser
    {
        public int AdUserId { get; set; }
        public string DisplayName { get; set; }
        public string ActiveDirectorySid { get; set; }
        public DateTime LastCheckedDate { get; set; }
    }
}
