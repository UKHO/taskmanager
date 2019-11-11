using System;
using System.Collections.Generic;
using WorkflowDatabase.EF.Models;

namespace Portal
{
    public static class OnHoldCalculator
    {
        /// <summary>
        /// Returns the number of days a task has been on hold.
        /// </summary>
        /// <param name="rowsToCalculate">All the OnHold rows from the DB for the task.</param>
        /// <returns></returns>
        public static int CalculateOnHoldDays(IEnumerable<OnHold> rowsToCalculate)
        {
            double onHoldTotal = 0;

            foreach (var row in rowsToCalculate)
            {
                if (row.OffHoldTime == null)
                {
                    onHoldTotal += (DateTime.Now.Date- row.OnHoldTime).TotalDays;
                }
                else
                {
                    onHoldTotal += (row.OffHoldTime.Value- row.OnHoldTime).TotalDays;
                }
            }

            return Convert.ToInt32(onHoldTotal);
        }
    }
}
