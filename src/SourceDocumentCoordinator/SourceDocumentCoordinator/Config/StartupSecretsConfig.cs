namespace SourceDocumentCoordinator.Config
{
    public class StartupSecretsConfig
    {
        public string ContentServiceUsername { get; set; }
        public string ContentServicePassword { get; set; }
        public string ContentServiceDomain { get; set; }
        public string SqlLoggingUsername { get; set; }
        public string SqlLoggingPassword { get; set; }
    }
}
