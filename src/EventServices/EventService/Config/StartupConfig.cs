using System;

namespace EventService.Config
{
    public class StartupConfig
    {
        public string SourceDocumentCoordinatorName { get; set; }
        public string WorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
    }
}
