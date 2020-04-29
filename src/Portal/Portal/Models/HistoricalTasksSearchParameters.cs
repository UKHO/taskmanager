using System.ComponentModel;

namespace Portal.Models
{
    public class HistoricalTasksSearchParameters
    {
        [DisplayName("Process Id:")]
        public int ProcessId { get; set; }
        [DisplayName("Sdoc Id:")]
        public int SourceDocumentId { get; set; }
        [DisplayName("RSDRA Number:")]
        public string RsdraNumber { get; set; }
        [DisplayName("Source Name:")]
        public string SourceDocumentName { get; set; }
        [DisplayName("Reviewer:")]
        public string Reviewer { get; set; }
        [DisplayName("Assessor:")]
        public string Assessor { get; set; }
        [DisplayName("Verifier:")]
        public string Verifier { get; set; }
    }
}
