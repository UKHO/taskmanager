using System;

namespace WorkflowDatabase.EF.Models
{
    public class DatabaseDocumentStatus
    {
        public int DatabaseDocumentStatusId { get; set; }
        public int ProcessId { get; set; }
        public int SdocId { get; set; }
        public string SourceDocumentName { get; set; }
        public string SourceDocumentType { get; set; }
        public Guid? ContentServiceId { get; set; }
        public string Status { get; set; }
        public DateTime Created { get; set; }

        public Uri ContentServiceUri { get; set; }
    }
}
