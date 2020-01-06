using System;
using Microsoft.Extensions.Options;
using Portal.Configuration;

namespace Portal.Calculators
{
    public class DmEndDateCalculator : IDmEndDateCalculator
    {
        private readonly IOptions<GeneralConfig> _generalConfig;

        public DmEndDateCalculator(IOptions<GeneralConfig> generalConfig)
        {
            _generalConfig = generalConfig;
        }

        public DateTime CalculateDmEndDate(DateTime effectiveStartDate)
        {
            return effectiveStartDate.AddDays(_generalConfig.Value.DmEndDateDays);
        }

        public short CalculateDaysToDmEndDate(DateTime sourceDate, DateTime dmEndDate)
        {
            return 0;
        }
    }
}
