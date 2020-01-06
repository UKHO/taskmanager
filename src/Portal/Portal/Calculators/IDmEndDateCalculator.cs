using System;

namespace Portal.Calculators
{
    public interface IDmEndDateCalculator
    {
        DateTime CalculateDmEndDate(DateTime effectiveStartDate);
        short CalculateDaysToDmEndDate(DateTime sourceDate, DateTime dmEndDate);
    }
}
