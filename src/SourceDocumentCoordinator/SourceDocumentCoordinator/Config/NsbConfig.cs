using System;

namespace SourceDocumentCoordinator.Config
{
    public class NsbConfig
    {
        public bool IsLocalDevelopment { get; set; }
        public string SourceDocumentCoordinatorName { get; set; }
        public string EventServiceName { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
    }
}
