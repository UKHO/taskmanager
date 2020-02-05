namespace Portal.Configuration
{
    public class GeneralConfig
    {
        public string CallerCode { get; set; }
        public string K2DBAssessmentWorkflowName { get; set; }
        public string AzureAdClientId { get; set; }
        public string TenantId { get; set; }
        public int DmEndDateDaysSimple { get; set; }
        public int DmEndDateDaysLTA { get; set; }
        public int DaysToDmEndDateRedAlertUpperInc { get; set; }
        public int DaysToDmEndDateAmberAlertUpperInc { get; set; }
        public string SessionFilename { get; set; }

    }
}