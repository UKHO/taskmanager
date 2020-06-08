using System;
using System.ComponentModel.DataAnnotations;

namespace NCNEPortal.Models
{
    public class HistoricalTasksData
    {

        public int? ProcessId { get; set; }

        public string ChartNo { get; set; }

        public string Country { get; set; }

        public string WorkflowType { get; set; }
        public string Compiler { get; set; }
        public string VerifierOne { get; set; }

        public string VerifierTwo { get; set; }

        public string Publisher { get; set; }

        public string Status { get; set; }

        [DisplayFormat(DataFormatString = "{0:d}")]
        public DateTime ActivityChangedAt { get; set; }

    }
}
