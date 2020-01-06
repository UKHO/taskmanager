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

        public DateTime CalculateDmEndDate(DateTime effectiveStartDate, IEnumerable<OnHold> onHoldRows)
        {
            var result = _dmEndDateCalculator.CalculateDmEndDate(effectiveStartDate);

            if (onHoldRows != null && onHoldRows.Any())
            {
                var onHoldDays = _onHoldCalculator.CalculateOnHoldDays(onHoldRows, DateTime.Now);
                result = result.AddDays(onHoldDays);
            }

            return result;
        }
    }
}
