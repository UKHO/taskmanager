using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class TaskStageType
    {
        [Key]
        [DatabaseGenerated(databaseGeneratedOption: DatabaseGeneratedOption.Identity)]
        public int TaskStageTypeId { get; set; }

        public string Name { get; set; }
        public int SequenceNumber { get; set; }
        public bool AllowRework { get; set; }

    }
}
