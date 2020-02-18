using System.ComponentModel.DataAnnotations.Schema;

namespace HpdDatabase.EF.Models
{
    [Table("VECTOR_PRODUCT_VIEW", Schema = "HPDOWNER")]
    public class CarisProduct
    {
        [Column("NAME")]
        public string ProductName { get; set; }

        [Column("PRODUCT_STATUS")]
        public string ProductStatus { get; set; }

        [Column("TYPE_KEY")]
        public string TypeKey { get; set; }
    }
}
