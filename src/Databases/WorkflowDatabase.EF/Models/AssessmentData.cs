using System;
using System.ComponentModel.DataAnnotations;

namespace WorkflowDatabase.EF.Models
{
    public class AssessmentData
    {
        public int AssessmentDataId { get; set; }
        public int ProcessId { get; set; }
        public int PrimarySdocId { get; set; }

        public string RsdraNumber { get; set; }

        [DisplayFormat(NullDisplayText = "N/A")]
        public string ParsedRsdraNumber => this.RsdraNumber.Replace("RSDRA", "");

        [DisplayFormat(NullDisplayText = "N/A")]
        public string SourceDocumentName { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public DateTime? ToSdoDate { get; set; }
        public DateTime? EffectiveStartDate { get; set; }
        public string TeamDistributedTo { get; set; }

        [DisplayFormat(NullDisplayText = "N/A")]
        public string SourceDocumentType { get; set; }

        [DisplayFormat(NullDisplayText = "N/A")]
        public string SourceNature { get; set; }

        [DisplayFormat(NullDisplayText = "N/A")]
        public string Datum { get; set; }
    }
}
    