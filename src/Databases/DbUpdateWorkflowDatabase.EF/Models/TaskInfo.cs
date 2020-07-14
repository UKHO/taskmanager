using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class TaskInfo
    {
        [Key]
        [DatabaseGenerated(databaseGeneratedOption: DatabaseGeneratedOption.Identity)]
        public int ProcessId { get; set; }
        public string Name { get; set; }

        public string UpdateType { get; set; }

        public string ChartingArea { get; set; }

        public DateTime? TargetDate { get; set; }

        public string CurrentStage { get; set; }


        public DateTime AssignedDate { get; set; }

        public string Status { get; set; }

        public DateTime? StatusChangeDate { get; set; }

        public virtual AdUser Assigned { get; set; }
        public int AssignedAdUserId { get; set; }
        public virtual TaskNote TaskNote { get; set; }

        public virtual TaskRole TaskRole { get; set; }

        public virtual List<TaskComment> TaskComment { get; set; }

        public virtual List<TaskStage> TaskStage { get; set; }
    }
}
