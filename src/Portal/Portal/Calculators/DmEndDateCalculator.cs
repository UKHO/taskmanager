﻿using System;
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

            var isReview = taskStage == "Review";
            var isSimple = taskType == "Simple";
            var dmEndDateDays = (isReview || isSimple)
                                                    ? _generalConfig.Value.DmEndDateDaysSimple
                                                    : _generalConfig.Value.DmEndDateDaysLTA;

            var dmEndDate = effectiveStartDate.AddDays(dmEndDateDays);
            var daysToDmEndDate = (short)dmEndDate.Date.Subtract(DateTime.Today).Days;

            return (dmEndDate: dmEndDate, daysToDmEndDate: daysToDmEndDate);
        }

        public (bool redAlert, bool amberAlert, bool greenAlert) DetermineDaysToDmEndDateAlerts(short daysToDmEndDate)
        {
            if (daysToDmEndDate > 999 || daysToDmEndDate < -999)
            {
                return (redAlert: false, amberAlert: false, greenAlert: false);
            }

            var redAlert = daysToDmEndDate <= _generalConfig.Value.DaysToDmEndDateRedAlertUpperInc;
            var amberAlert = !redAlert && daysToDmEndDate <= _generalConfig.Value.DaysToDmEndDateAmberAlertUpperInc;
            var greenAlert = !redAlert && !amberAlert;

            return (redAlert: redAlert, amberAlert: amberAlert, greenAlert: greenAlert);
        }
    }
}
