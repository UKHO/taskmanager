using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("SOURCEREGISTRY_PRIORITY", Schema = "HPDOWNER")]
    public class CarisProjectPriority
    {
        [Column("SOURCEREG_PRIORITY_ID")]
        public int ProjectPriorityId { get; set; }

        [Column("NAME")]
        public string ProjectPriorityName { get; set; } 
    }
}
    