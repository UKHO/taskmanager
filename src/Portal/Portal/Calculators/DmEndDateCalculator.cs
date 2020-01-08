using System;
using Microsoft.Extensions.Options;
using Portal.Configuration;

namespace Portal.Calculators
{
    public class DmEndDateCalculator : IDmEndDateCalculator
    {
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
            var redAlert = daysToDmEndDate <= _generalConfig.Value.DaysToDmEndDateRedAlertUpperInc; 
            var amberAlert = !redAlert && daysToDmEndDate <= _generalConfig.Value.DaysToDmEndDateAmberAlertUpperInc;

            return (redAlert: redAlert, amberAlert: amberAlert);
        }
    }
}
