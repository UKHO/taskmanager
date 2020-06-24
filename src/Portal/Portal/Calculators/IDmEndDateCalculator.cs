using System;

namespace Portal.Calculators
{
    public interface IDmEndDateCalculator
    {
        (DateTime dmEndDate, short daysToDmEndDate) CalculateDmEndDate(DateTime effectiveStartDate, string taskType, string taskStage);
        (bool redAlert, bool amberAlert, bool greenAlert) DetermineDaysToDmEndDateAlerts(short daysToDmEndDate);
    }
}
