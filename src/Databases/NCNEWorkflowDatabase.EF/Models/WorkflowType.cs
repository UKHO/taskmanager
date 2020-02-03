using System.ComponentModel;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class WorkflowType
    {
        public int WorkflowTypeId { get; set; }

        [DisplayName("Workflow Type")]
        public string Name { get; set; }
    }
}
