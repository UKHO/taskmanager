﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbUpdateWorkflowDatabase.EF.Models
{
    public class TaskStage
    {

        [DatabaseGenerated(databaseGeneratedOption: DatabaseGeneratedOption.Identity)]
        public int TaskStageId { get; set; }
        public int ProcessId { get; set; }
        public int TaskStageTypeId { get; set; }

        public DateTime? DateExpected { get; set; }

        public DateTime? DateCompleted { get; set; }
        public string Status { get; set; }

        public virtual List<TaskStageComment> TaskStageComment { get; set; }

        public virtual TaskStageType TaskStageType { get; set; }

        public virtual bool IsReadOnly { get; set; } = false;

        public virtual AdUser Assigned { get; set; }
        public int? AssignedAdUserId { get; set; }
    }

}
