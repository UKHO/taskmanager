using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class AssignedTaskSourceType
    {
        public int AssignedTaskSourceTypeId { get; set; }

        [DisplayName("Source Type:")]
        public string Name { get; set; }
    }
}
