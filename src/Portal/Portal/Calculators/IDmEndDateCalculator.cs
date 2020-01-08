using System;

namespace Portal.Calculators
{
    public interface IDmEndDateCalculator
    {
        (DateTime dmEndDate, short daysToDmEndDate) CalculateDmEndDate(DateTime effectiveStartDate);
        (bool redAlert, bool amberAlert) DetermineDaysToDmEndDateAlerts(short daysToDmEndDate);
    }
}
