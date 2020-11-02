using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class SncAction
    {
        public int SncActionId { get; set; }

        public int ProcessId { get; set; }

        [DisplayName("Impacted Product:")]
        public string ImpactedProduct { get; set; }

        [DisplayName("Product Action Type:")]
        public int SncActionTypeId { get; set; }

        [DisplayName("Verified:")]
        public bool Verified { get; set; }

        public virtual SncActionType SncActionType { get; set; }
    }
}
