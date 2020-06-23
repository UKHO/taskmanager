namespace DbUpdatePortal.Configuration
{
    public class GeneralConfig
    {
        public string AzureAdClientId { get; set; }
        public string TenantId { get; set; }
        public string CarisNewProjectStatus { get; set; }
        public string CarisNewProjectPriority { get; set; }
        public string CarisDbUpdateProjectType { get; set; }
        public int CarisProjectTimeoutSeconds { get; set; }
        public int HistoricalTasksInitialNumberOfRecords { get; set; }
    }
}