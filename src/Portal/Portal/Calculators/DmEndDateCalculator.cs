using System;
using Microsoft.Extensions.Options;
using Portal.Configuration;

namespace Portal.Calculators
{
    public class DmEndDateCalculator : IDmEndDateCalculator
    {
        private const int DAYS_TO_DM_END_DATE_RED_ALERT_UPPER_INC = 0; //TODO: CONFIG
        private const int DAYS_TO_DM_END_DATE_AMBER_ALERT_UPPER_INC = 2; //TODO: CONFIG

        private readonly IOptions<GeneralConfig> _generalConfig;

        public DmEndDateCalculator(IOptions<GeneralConfig> generalConfig)
        {
            _generalConfig = generalConfig;
        }

        public (DateTime dmEndDate, short daysToDmEndDate) CalculateDmEndDate(DateTime effectiveStartDate)
        {
            var dmEndDate = effectiveStartDate.AddDays(_generalConfig.Value.DmEndDateDays);
            var daysToDmEndDate = (short)dmEndDate.Date.Subtract(DateTime.Today).Days;

            return (dmEndDate: dmEndDate, daysToDmEndDate: daysToDmEndDate);
        }

        public (bool redAlert, bool amberAlert) DetermineDaysToDmEndDateAlerts(short daysToDmEndDate)
        {
            var redAlert = daysToDmEndDate <= DAYS_TO_DM_END_DATE_RED_ALERT_UPPER_INC; 
            var amberAlert = !redAlert && daysToDmEndDate <= DAYS_TO_DM_END_DATE_AMBER_ALERT_UPPER_INC;

            return (redAlert: redAlert, amberAlert: amberAlert);
        }
    }
}
