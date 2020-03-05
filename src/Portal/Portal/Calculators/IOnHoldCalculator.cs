using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal.Calculators
{
    public interface IOnHoldCalculator
    {
        int CalculateOnHoldDays(IEnumerable<OnHold> rowsToCalculate, DateTime currentDate);
        (bool greenIcon, bool amberIcon, bool redIcon) DetermineOnHoldDaysIcons(int onHoldDays);
    }
}
