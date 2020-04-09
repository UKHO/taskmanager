using System;

namespace WorkflowDatabase.EF.Models
{
    public class PrimaryDocumentStatus
    {
        public int PrimaryDocumentStatusId { get; set; }
        public int ProcessId { get; set; }
        public int SdocId { get; set; }
        public Guid? ContentServiceId { get; set; }
        public string Status { get; set; }
        public DateTime StartedAt { get; set; }
        public Guid? CorrelationId { get; set; }
        public string Filename { get; set; }
        public string Filepath { get; set; }
    }
}
