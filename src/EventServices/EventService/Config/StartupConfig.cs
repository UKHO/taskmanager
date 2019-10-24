using System;

namespace EventService.Config
{
    public class StartupConfig
    {
        public string EventServiceName { get; set; }
        public string LocalDbServer { get; set; }
        public Uri AzureDbTokenUrl { get; set; }
    }
}
