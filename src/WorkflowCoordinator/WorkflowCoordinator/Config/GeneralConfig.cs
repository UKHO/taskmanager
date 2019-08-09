using System;

namespace WorkflowCoordinator.Config
{
    public class GeneralConfig
    {
        public Uri DataAccessLocalhostBaseUri { get; set; }

        public string SourceDocumentCoordinatorName { get; set; }

        public Uri AzureDbTokenUrl { get; set; }
    }
}
