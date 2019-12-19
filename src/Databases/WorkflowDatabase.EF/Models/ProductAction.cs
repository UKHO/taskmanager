using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class ProductAction
    {
        public int ProductActionId { get; set; }

        public int ProcessId { get; set; }

        [DisplayName("Impacted Product:")]
        public string ImpactedProduct { get; set; }

        [DisplayName("Product Action Type:")]
        public int ProductActionTypeId { get; set; }

        [DisplayName("Verified:")]
        public bool Verified { get; set; }

        public virtual ProductActionType ProductActionType { get; set; }
    }
}
