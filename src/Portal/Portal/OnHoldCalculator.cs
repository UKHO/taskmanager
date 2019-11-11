using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal
{
    public class OnHoldCalculator : IOnHoldCalculator
    {
        public OnHoldCalculator()
        {
            
        }

        /// <summary>
        /// Returns the number of days a task has been on hold.
        /// </summary>
        /// <param name="rowsToCalculate">All the OnHold rows from the DB for the task.</param>
        /// <param name="currentDate">The current date.</param>
        /// <returns></returns>
        public int CalculateOnHoldDays(IEnumerable<OnHold> rowsToCalculate, DateTime currentDate)
        {
            double onHoldTotal = 0;

            foreach (var row in rowsToCalculate)
            {
                if (row.OffHoldTime == null)
                {
                    onHoldTotal += (currentDate.Date - row.OnHoldTime.Date).TotalDays;
                }
                else
                {
                    onHoldTotal += (row.OffHoldTime.Value.Date - row.OnHoldTime.Date).TotalDays;
                }
            }

            return Convert.ToInt32(onHoldTotal);
        }
    }
}
