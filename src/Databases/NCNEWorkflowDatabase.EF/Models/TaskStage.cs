using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class TaskStage
    {

        [DatabaseGenerated(databaseGeneratedOption: DatabaseGeneratedOption.Identity)]
        public int TaskStageId { get; set; }
        public int ProcessId { get; set; }
        public int TaskStageTypeId { get; set; }

        public string AssignedUser { get; set; }
        public DateTime? DateExpected { get; set; }

        public DateTime? DateCompleted { get; set; }
        public string Status { get; set; }

        public virtual List<TaskStageComment> TaskStageComment { get; set; }

        public virtual TaskStageType TaskStageType { get; set; }

        [NotMapped]
        public virtual bool IsReadOnly { get; set; } = false;
    }

}
