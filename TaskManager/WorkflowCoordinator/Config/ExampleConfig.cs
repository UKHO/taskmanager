using System;

namespace WorkflowCoordinator.Config
{
    public class ExampleConfig
    {
        public string NsbEndpointName { get; set; }
        public ConnectionStrings ConnectionStrings { get; set; }
    }

    public class ConnectionStrings
    {
        public Uri AzureDbTokenUrl { get; set; }
    }

    // e.g.
    //public IEnumerable<string> Validate()
    //{
    //     if ....
    //     yield return $"Valid ... not ... {nameof(MySecret)}";
    //}
}
