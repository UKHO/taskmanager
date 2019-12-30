using System;

namespace Portal.Configuration
{
    public class StartupLoggingConfig
    {
        public string WorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public string LocalDbName { get; set; }
    }
}
