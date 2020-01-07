namespace EventService.Config
{
    public class StartupSecretsConfig
    {
        public string NsbDataSource { get; set; }
        public string NsbInitialCatalog { get; set; }
        public string SqlLoggingUsername { get; set; }
        public string SqlLoggingPassword { get; set; }
    }
}
