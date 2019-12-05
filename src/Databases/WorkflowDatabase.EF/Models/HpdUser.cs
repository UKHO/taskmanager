using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("HpdUser")]
    public class HpdUser
    {
        public int HpdUserId { get; set; }
        public string AdUsername { get; set; }
        public string HpdUsername { get; set; }
    }
}
