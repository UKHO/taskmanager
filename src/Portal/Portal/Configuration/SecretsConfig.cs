using System;

namespace Portal.Configuration
{
    public class SecretsConfig
    {
        public string ClientAzureAdSecret { get; set; }
        public Guid HDTGuid { get; set; }
        public string HpdServiceName { get; set; }
        public Guid HDCGuid { get; set; }
    }
}