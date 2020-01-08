using System;

namespace SourceDocumentCoordinator.Config
{
    public class StartupConfig
    {
        public string WorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
    }
}
