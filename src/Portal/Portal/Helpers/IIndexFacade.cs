using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public interface IIndexFacade
    {
        int CalculateOnHoldDays(IEnumerable<OnHold> onHoldRows);
        (DateTime dmEndDate, short daysToDmEndDate) CalculateDmEndDate(DateTime effectiveStartDate, string taskType, string taskStage, IEnumerable<OnHold> onHoldRows);
        (bool redAlert, bool amberAlert) DetermineDaysToDmEndDateAlerts(short daysToDmEndDate);
    }
}
