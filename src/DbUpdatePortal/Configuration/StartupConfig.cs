using System;

namespace DbUpdatePortal.Configuration
{
    public class StartupConfig
    {
        public string DbUpdateWorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public string TenantId { get; set; }
        public string AzureAdClientId { get; set; }
        public int CookieTimeoutHours { get; set; }
    }
}
