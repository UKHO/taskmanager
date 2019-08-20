using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace WorkflowDatabase.EF.Models
{
    public class WorkflowInstance
    {
        public int WorkflowInstanceId { get; set; }
        public int ProcessId { get; set; }
        public string SerialNumber { get; set; }
        public int? ParentProcessId { get; set; }
        public string WorkflowType { get; set; }
        public string ActivityName { get; set; }

        public virtual List<Comment> Comment { get; set; }
    }
}
