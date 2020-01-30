using System.ComponentModel;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class ChartType
    {
        public int ChartTypeId { get; set; }

        [DisplayName("Chart Type")]
        public string Name { get; set; }
    }
}
