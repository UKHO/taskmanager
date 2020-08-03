using System.ComponentModel;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class ProductAction
    {
        public int ProductActionId { get; set; }

        [DisplayName("Product Action")]
        public string Name { get; set; }
    }
}
