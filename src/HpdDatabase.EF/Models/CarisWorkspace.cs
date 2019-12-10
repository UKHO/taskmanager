using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("HPD_WORKSPACES_VW", Schema = "HPDOWNER")]
    public class CarisWorkspace
    {
        [Column("WS_NAME")]
        public string Name { get; set; }
    }
}
