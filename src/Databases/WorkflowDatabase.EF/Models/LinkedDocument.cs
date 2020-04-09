using System;

namespace WorkflowDatabase.EF.Models
{
    public class LinkedDocument
    {
        public int LinkedDocumentId { get; set; }
        public int ProcessId { get; set; }
        public int PrimarySdocId { get; set; }
        public int LinkedSdocId { get; set; }
        public string LinkType { get; set; }
        public string RsdraNumber { get; set; }
        public string ParsedRsdraNumber => this.RsdraNumber.Replace("RSDRA", "");
        public string SourceDocumentName { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public string SourceDocumentType { get; set; }
        public string SourceNature { get; set; }
        public string Datum { get; set; }
        public Guid? ContentServiceId { get; set; }
        public string Status { get; set; }
        public DateTime Created { get; set; }
        public Uri ContentServiceUri { get; set; }
        public string Filename { get; set; }
        public string Filepath { get; set; }
    }
}
