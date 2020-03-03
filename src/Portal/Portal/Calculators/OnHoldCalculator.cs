using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Portal.Configuration;
using WorkflowDatabase.EF.Models;

namespace Portal.Calculators
{
    public class OnHoldCalculator : IOnHoldCalculator
    {
        private readonly IOptions<GeneralConfig> _generalConfig;

        public OnHoldCalculator(IOptions<GeneralConfig> generalConfig)
        {
            _generalConfig = generalConfig;
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

        public (bool greenIcon, bool amberIcon, bool redIcon) DetermineOnHoldDaysIcons(int onHoldDays)
        {
            var redIcon = onHoldDays >= _generalConfig.Value.OnHoldDaysRedIconUpper;
            var amberIcon = !redIcon && onHoldDays == _generalConfig.Value.OnHoldDaysAmberIconUpper;
            var greenIcon = !amberIcon && !redIcon && onHoldDays <= _generalConfig.Value.OnHoldDaysGreenIconUpper;

            return (greenIcon: greenIcon, amberIcon: amberIcon, redIcon: redIcon);
        }
    }
}
