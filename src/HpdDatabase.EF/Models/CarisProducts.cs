using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("VECTOR_PRODUCT_VIEW", Schema = "HPDOWNER")]
    public class CarisProducts
    {
        [Column("name")]
        public string ProductName { get; set; }

        [Column("product_status")]
        public string ProductStatus { get; set; }

        [Column("Type_key")]
        public string TypeKey { get; set; }
    }
}
