using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("CachedHpdWorkspace")]
    public class CachedHpdWorkspace
    {
        public int CachedHpdWorkspaceId { get; set; }

        [DisplayName("Workspace Name:")]
        public string Name { get; set; }
    }
}
