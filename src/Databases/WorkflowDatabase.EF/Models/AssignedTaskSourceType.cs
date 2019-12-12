using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkflowDatabase.EF.Models
{
    [Table("AssignedTaskSourceType")]
    public class AssignedTaskSourceType
    {
        public int AssignedTaskSourceTypeId { get; set; }

        [DisplayName("Source Type:")]
        public string Name { get; set; }
    }
}
