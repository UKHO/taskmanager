using System;

namespace WorkflowCoordinator.Config
{
    public class GeneralConfig
    {
        public ConnectionStrings ConnectionStrings { get; set; }
    }

    public class ConnectionStrings
    {
        public Uri AzureDbTokenUrl { get; set; }
    }
}
