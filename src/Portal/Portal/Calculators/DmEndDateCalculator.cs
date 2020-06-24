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

        public (DateTime dmEndDate, short daysToDmEndDate) CalculateDmEndDate(DateTime effectiveStartDate, string taskType, string taskStage)
        {

            var isReview = taskStage.Equals("Review", StringComparison.InvariantCultureIgnoreCase);
            var isSimple = taskType.Equals("Simple", StringComparison.InvariantCultureIgnoreCase);
            var dmEndDateDays = (isReview || isSimple)
                                                    ? _generalConfig.Value.DmEndDateDaysSimple
                                                    : _generalConfig.Value.DmEndDateDaysLTA;

            var dmEndDate = effectiveStartDate.AddDays(dmEndDateDays);
            var daysToDmEndDate = (short)dmEndDate.Date.Subtract(DateTime.Today).Days;

            return (dmEndDate: dmEndDate, daysToDmEndDate: daysToDmEndDate);
        }

        public (bool redAlert, bool amberAlert, bool greenAlert) DetermineDaysToDmEndDateAlerts(short daysToDmEndDate)
        {
            var redAlert = daysToDmEndDate <= _generalConfig.Value.DaysToDmEndDateRedAlertUpperInc; 
            var amberAlert = !redAlert && daysToDmEndDate <= _generalConfig.Value.DaysToDmEndDateAmberAlertUpperInc;
            var greenAlert = !redAlert && !amberAlert;

            return (redAlert: redAlert, amberAlert: amberAlert, greenAlert: greenAlert);
        }
    }
}
