using System.ComponentModel.DataAnnotations;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class TaskStageType
    {
        [Key]
        public int TaskStageTypeId { get; set; }

        public string Name { get; set; }
        public int SequenceNumber { get; set; }
        public bool AllowRework { get; set; }

        public virtual string DisplayName { get; set; }

    }
}
