using Microsoft.Extensions.Options;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using System;
using System.Globalization;

namespace NCNEPortal.Calculators
{
    public class MilestoneCalculator : IMilestoneCalculator
    {
        private readonly IOptions<GeneralConfig> _generalConfig;

        public MilestoneCalculator(IOptions<GeneralConfig> generalConfig)
        {
            this._generalConfig = generalConfig;
        }

        public (DateTime formsDate, DateTime cisDate, DateTime commitDate) CalculateMilestones(DeadlineEnum deadline,
            DateTime publicationDate)
        {


            var dtForms = publicationDate.AddDays(_generalConfig.Value.FormsDaysFromPubDate);
            var dtCis = publicationDate.AddDays(_generalConfig.Value.CisDaysFromPubDate);
            var dtCommit = (deadline == DeadlineEnum.TwoWeeks ?
                publicationDate.AddDays(_generalConfig.Value.Commit2WDaysFromPubDate) :
                publicationDate.AddDays(_generalConfig.Value.Commit3WDaysFromPubDate));

            return (formsDate: dtForms, cisDate: dtCis, commitDate: dtCommit);

        }
    }
}
