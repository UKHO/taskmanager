using System;

namespace WorkflowDatabase.EF.Models
{
    public class SourceDocumentStatus
    {
        public int SourceDocumentStatusId { get; set; }
        public int ProcessId { get; set; }
        public int SdocId { get; set; }
        public Guid ContentServiceId { get; set; }
        public string Status { get; set; }
        public DateTime StartedAt { get; set; }
    }
}
