using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("ProductAction")]

    public class ProductAction
    {
        public int ProductActionId { get; set; }
        public int ProcessId { get; set; }
        [DisplayName("Impacted Product:")]
        public string ImpactedProduct { get; set; }

        public int ProductActionTypeId { get; set; }

        [DisplayName("Verified:")]
        public bool Verified { get; set; }
        
        public virtual ProductActionType ProductActionType { get; set; }

    }
}
