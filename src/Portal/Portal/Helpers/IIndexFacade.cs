using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public interface IIndexFacade
    {
        DateTime CalculateDmEndDate(DateTime effectiveStartDate, IEnumerable<OnHold> onHoldRows);
        short CalculateDaysToDmEndDate(DateTime sourceDate, DateTime dmEndDate);
    }
}
