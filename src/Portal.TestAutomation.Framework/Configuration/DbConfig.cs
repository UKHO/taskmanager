using System;

namespace Portal.TestAutomation.Framework.Configuration
{
    public class DbConfig
    {
        public string WorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public string WorkflowDbUITestAcct { get; set; }
        public string WorkflowDbPassword { get; set; }
    }
}
