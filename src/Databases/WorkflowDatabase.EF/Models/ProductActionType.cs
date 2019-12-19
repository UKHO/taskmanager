using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class ProductActionType
    {
        public int ProductActionTypeId { get; set; }

        [DisplayName("Action Type:")]
        public string Name { get; set; }
    }
}
