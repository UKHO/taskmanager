using System;
using System.ComponentModel.DataAnnotations;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class NcneTaskInfo
    {
        [Key]
        public int ProcessId { get; set; }
        public string Ion { get; set; }
        public string Country { get; set; }
        public string ChartType { get; set; }
        public string WorkflowType { get; set; }
        public string Dating { get; set; }
        public DateTime PublicationDate { get; set; }
        public DateTime AnnounceDate { get; set; }
        public DateTime CommitDate { get; set; }
        public DateTime CisDateTime { get; set; }
        public string Compiler { get; set; }
        public string VerifierOne { get; set; }
        public string VerifierTwo { get; set; }
        public string Publisher { get; set; }
    }
}