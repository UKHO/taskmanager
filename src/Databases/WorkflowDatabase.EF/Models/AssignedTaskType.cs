using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class AssignedTaskType
    {
        public int AssignedTaskTypeId { get; set; }

        [DisplayName("Task Type:")]
        public string Name { get; set; }
    }
}
