using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("hydrodbusers", Schema = "HPDOWNER")]
    public class CarisUser
    {
        [Column("HYDRODBUSERS_ID")]
        public int UserId { get; set; }

        [Column("USERNAME")]
        public string Username { get; set; } 
    }
}
    