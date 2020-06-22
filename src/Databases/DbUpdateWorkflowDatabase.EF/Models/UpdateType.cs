using System.ComponentModel;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class UpdateType
    {
        public int UpdateTypeId { get; set; }

        [DisplayName("Update Type")]
        public string Name { get; set; }
    }
}
