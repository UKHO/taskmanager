using System;
using System.ComponentModel.DataAnnotations;

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

        [DisplayFormat(NullDisplayText = "N/A")]
        public string ParsedRsdraNumber => this.RsdraNumber?.Replace("RSDRA", "");

        [DisplayFormat(NullDisplayText = "N/A")]
        public string SourceDocumentName { get; set; }

        [DisplayFormat(NullDisplayText = "N/A", DataFormatString = "{0:d}")]
        public DateTime? ReceiptDate { get; set; }

        [DisplayFormat(NullDisplayText = "N/A")]
        public string SourceDocumentType { get; set; }

        [DisplayFormat(NullDisplayText = "N/A")]
        public string SourceNature { get; set; }

        [DisplayFormat(NullDisplayText = "N/A")]
        public string Datum { get; set; }
        public Guid? ContentServiceId { get; set; }
        public string Status { get; set; }
        public DateTime Created { get; set; }
        public Uri ContentServiceUri { get; set; }
        public string Filename { get; set; }
        public string Filepath { get; set; }
        public Guid UniqueId { get; set; }
    }
}
