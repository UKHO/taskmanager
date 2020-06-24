using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public interface IIndexFacade
    {
        int CalculateOnHoldDays(IEnumerable<OnHold> onHoldRows);
        (bool greenIcon, bool amberIcon, bool redIcon) DetermineOnHoldDaysIcons(int onHoldDays);
        (DateTime dmEndDate, short daysToDmEndDate) CalculateDmEndDate(DateTime effectiveStartDate, string taskType, string taskStage, IEnumerable<OnHold> onHoldRows);
        (bool redAlert, bool amberAlert, bool greenAlert) DetermineDaysToDmEndDateAlerts(short daysToDmEndDate);
    }
}
