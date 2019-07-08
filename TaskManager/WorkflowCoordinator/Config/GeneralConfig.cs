using System;

namespace WorkflowCoordinator.Config
{
    public class GeneralConfig
    {
        public string NsbEndpointName { get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
    }

    public class ConnectionStrings
    {
        public Uri AzureDbTokenUrl { get; set; }
    }
}
