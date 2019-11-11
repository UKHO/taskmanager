using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal
{
    public interface IOnHoldCalculator
    {
        int CalculateOnHoldDays(IEnumerable<OnHold> rowsToCalculate, DateTime currentDate);
    }
}
