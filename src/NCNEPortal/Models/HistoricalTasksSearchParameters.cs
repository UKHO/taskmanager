using System.ComponentModel;

namespace NCNEPortal.Models
{
    public class HistoricalTasksSearchParameters
    {
        [DisplayName("Process Id:")]
        public int? ProcessId { get; set; }

        [DisplayName("Chart number")]
        public string ChartNo { get; set; }

        [DisplayName("Country")]
        public string Country { get; set; }

        [DisplayName("Chart type")] public string ChartType { get; set; }

        [DisplayName("Workflow type")] public string WorkflowType { get; set; }
        [DisplayName("Compiler")] public string Compiler { get; set; }
        [DisplayName("Verifier 1")] public string VerifierOne { get; set; }

        [DisplayName("Verifier 2")] public string VerifierTwo { get; set; }

        [DisplayName("Publisher")] public string Publisher { get; set; }

    }
}
