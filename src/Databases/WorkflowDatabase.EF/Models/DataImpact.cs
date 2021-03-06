﻿using System.ComponentModel;

namespace WorkflowDatabase.EF.Models
{
    public class DataImpact
    {
        public int DataImpactId { get; set; }
        public int ProcessId { get; set; }
        public int HpdUsageId { get; set; }
        public bool Edited { get; set; }
        public string Comments { get; set; }
        public bool FeaturesSubmitted { get; set; }
        public bool FeaturesVerified { get; set; }
        public bool StsUsage { get; set; }
        public virtual HpdUsage HpdUsage { get; set; }
    }
}
