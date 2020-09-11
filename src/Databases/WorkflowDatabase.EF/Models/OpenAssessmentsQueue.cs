using System;

namespace WorkflowDatabase.EF.Models
{
    public class OpenAssessmentsQueue
    {
        public int OpenAssessmentsQueueId { get; set; }
        public int PrimarySdocId { get; set; }
        public DateTime Timestamp { get; set; } 
    }
}
