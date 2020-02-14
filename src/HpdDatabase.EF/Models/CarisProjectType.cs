using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("PROJECT_TYPE", Schema = "HPDOWNER")]
    public class CarisProjectType
    {
        [Column("PROJECT_TYPE_ID")]
        public int ProjectTypeId { get; set; }

        [Column("NAME")]
        public string ProjectTypeName { get; set; } 
    }
}
