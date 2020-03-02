using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph;

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
        public string CarisNewProjectStatus { get; set; }
        public string CarisNewProjectPriority { get; set; }
        public string CarisNewProjectType { get; set; }
        public int CarisProjectTimeoutSeconds { get; set; }

        public string TeamsAsCsv { get; set; }
        public string TeamsUnassigned { get; set; }

        public IEnumerable<string> GetTeams()
        {
            // Teams will be stored in Azure App Config as a CSV:
            // HDB Master Data,HDBPR1

            if (string.IsNullOrWhiteSpace(TeamsAsCsv)) return null;

            var teamList = TeamsAsCsv.Split(',');

            if (teamList == null || teamList.Length == 0) return null;

            return teamList.Select(t => t.Trim());
        }

    }
}   