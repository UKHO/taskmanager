using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class ProductAction
    {
        public int ProductActionId { get; set; }
        public int ProcessId { get; set; }
        [DisplayName("Impacted Product:")]
        public string ImpactedProduct { get; set; }
        [DisplayName("Action Type:")]
        public string  ActionType { get; set; }
        [DisplayName("Verified:")]
        public bool Verified { get; set; }

    }
}
