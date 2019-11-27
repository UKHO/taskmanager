using System;

namespace Portal.Configuration
{
    public class StartupConfig
    {
        public string WorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public string TenantId { get; set; }
        public string AzureAdClientId { get; set; }
    }
}
