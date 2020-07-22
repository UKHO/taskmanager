using System;

namespace WorkflowCoordinator.Config
{
    public class NsbConfig
    {
        public bool IsLocalDevelopment { get; set; }
        public string WorkflowCoordinatorName { get; set; }
        public string SourceDocumentCoordinatorName { get; set; }
        public string EventServiceName { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
        public string ServiceControlQueue { get; set; }
        public Guid WorkflowCoordinatorUniqueIdentifier { get; set; }
    }
}
