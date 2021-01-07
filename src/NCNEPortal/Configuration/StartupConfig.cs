﻿using System;

namespace NCNEPortal.Configuration
{
    public class StartupConfig
    {
        public string NcneWorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public string TenantId { get; set; }
        public string AzureAdClientId { get; set; }
        public int CookieTimeoutHours { get; set; }
    }
}
