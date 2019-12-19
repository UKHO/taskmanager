using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class CachedHpdWorkspace
    {
        public int CachedHpdWorkspaceId { get; set; }

        [DisplayName("Workspace Name:")]
        public string Name { get; set; }
    }
}
