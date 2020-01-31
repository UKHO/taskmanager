using System.ComponentModel.DataAnnotations;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class TaskStageType
    {
        [Key]
        public int TaskStageTypeId { get; set; }

        public string Name { get; set; }
        public int SequenceNumber { get; set; }
        public bool AllowRework { get; set; }

    }
}
