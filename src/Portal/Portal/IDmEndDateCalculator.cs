using System;

namespace Portal
{
    public interface IDmEndDateCalculator
    {
        DateTime CalculateDmEndDate(DateTime effectiveStartDate);
    }
}
