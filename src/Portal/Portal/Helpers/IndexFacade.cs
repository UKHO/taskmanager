using System;
using System.Collections.Generic;
using System.Linq;
using Portal.Calculators;
using WorkflowDatabase.EF.Models;

namespace Portal.Helpers
{
    public class IndexFacade : IIndexFacade
    {
        protected IDmEndDateCalculator _dmEndDateCalculator;
        protected IOnHoldCalculator _onHoldCalculator;

        public IndexFacade(IDmEndDateCalculator dmEndDateCalculator, IOnHoldCalculator onHoldCalculator)
        {
            _dmEndDateCalculator = dmEndDateCalculator;
            _onHoldCalculator = onHoldCalculator;
        }

        public int CalculateOnHoldDays(IEnumerable<OnHold> onHoldRows)
        {
            return _onHoldCalculator.CalculateOnHoldDays(onHoldRows, DateTime.Now.Date);
        }

        public (DateTime dmEndDate, short daysToDmEndDate) CalculateDmEndDate(DateTime effectiveStartDate, IEnumerable<OnHold> onHoldRows)
        {
            var result = _dmEndDateCalculator.CalculateDmEndDate(effectiveStartDate);
            var dmEndDate = result.dmEndDate.Date;
            var daysToDmEndDate = result.daysToDmEndDate;

            if (onHoldRows != null && onHoldRows.Any())
            {
                var onHoldDays = _onHoldCalculator.CalculateOnHoldDays(onHoldRows, DateTime.Now.Date);
                dmEndDate = dmEndDate.AddDays(onHoldDays);
                daysToDmEndDate += (short)onHoldDays;
            }

            return (dmEndDate: dmEndDate, daysToDmEndDate: daysToDmEndDate);
        }

        public (bool redAlert, bool amberAlert) DetermineDaysToDmEndDateAlerts(short daysToDmEndDate)
        {
            return _dmEndDateCalculator.DetermineDaysToDmEndDateAlerts(daysToDmEndDate);
        }

    }
}
