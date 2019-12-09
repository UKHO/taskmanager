using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("ProductActionType")]
    public class ProductActionType
    {
        public int ProductActionTypeId { get; set; }

        [DisplayName("Action Type:")]
        public string Name { get; set; }
    }
}
