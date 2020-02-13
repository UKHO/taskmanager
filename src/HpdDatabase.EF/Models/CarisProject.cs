using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("HPD_PROJECT_VW", Schema = "HPDOWNER")]
    public class CarisProject
    {
        [Column("PJ_ID")]
        public int ProjectId { get; set; }

        [Column("PJ_NAME")]
        public string ProjectName { get; set; } 
    }
}
