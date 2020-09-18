using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("HPD_USER", Schema = "HPDOWNER")]
    public class CarisUser
    {
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Column("USERNAME")]
        public string Username { get; set; }
    }
}
