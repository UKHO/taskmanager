using System;
using System.Collections.Generic;
using System.Text;

namespace WorkflowDatabase.EF.Models
{
    public class DbAssessmentAssessData
    {
        public int DbAssessmentAssessDataId { get; set; }
        public int ProcessId { get; set; }
        public bool Action { get; set; }
        public string ChangeDetails { get; set; }
    }
}
