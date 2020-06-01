namespace WorkflowCoordinator.Config
{
    public class StartupSecretsConfig
    {
        public string K2RestApiUsername { get; set; }
        public string K2RestApiPassword { get; set; }
        public string SqlLoggingUsername { get; set; }
        public string SqlLoggingPassword { get; set; }
        public string PCPEventServiceUsername { get; set; }
        public string PCPEventServicePassword { get; set; }
    }
}
