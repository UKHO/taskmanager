using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NCNEWorkflowDatabase.EF.Models
{
    public class NcneTaskInfo
    {
        [Key]
        [DatabaseGenerated(databaseGeneratedOption: DatabaseGeneratedOption.Identity)]
        public int ProcessId { get; set; }
        public string Ion { get; set; }
        public int ChartNumber { get; set; }
        public string Country { get; set; }
        public string ChartType { get; set; }
        public string WorkflowType { get; set; }
        public string Dating { get; set; }
        public DateTime PublicationDate { get; set; }
        public DateTime AnnounceDate { get; set; }
        public DateTime CommitDate { get; set; }
        public DateTime CisDate { get; set; }
        public string Compiler { get; set; }
        public string VerifierOne { get; set; }
        public string VerifierTwo { get; set; }
        public string Publisher { get; set; }

        public virtual NcneTaskNote NcneTaskNote { get; set; }

    }
}