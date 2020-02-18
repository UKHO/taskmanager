using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("PROJECT_STATUS", Schema = "HPDOWNER")]
    public class CarisProjectStatus
    {
        [Column("PROJECT_STATUS_ID")]
        public int ProjectStatusId { get; set; }

        [Column("NAME")]
        public string ProjectStatusName { get; set; } 
    }
}
