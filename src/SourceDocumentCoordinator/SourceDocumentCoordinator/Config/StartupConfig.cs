using System;

namespace SourceDocumentCoordinator.Config
{
    public class StartupConfig
    {
        public string WorkflowDbName { get; set; }
        public string WorkflowDbServer { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }

        // NSB startup use for now
        public bool IsLocalDevelopment { get; set; }
        public string SourceDocumentCoordinatorName { get; set; }
        public string EventServiceName { get; set; }


    }
}
