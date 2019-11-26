using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("HpdUsage")]
    public class HpdUsage
    {
        public int HpdUsageId { get; set; }
        public string Name { get; set; }
    }
}
