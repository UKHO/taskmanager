using Microsoft.Extensions.Options;
using NCNEPortal.Configuration;
using NCNEPortal.Enums;
using System;

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

            var sPubDate = publicationDate.ToLongDateString();

            var dtForms = DateTime.Parse(sPubDate).AddDays(_generalConfig.Value.FormsDaysFromPubDate);
            var dtCis = DateTime.Parse(sPubDate).AddDays(_generalConfig.Value.CisDaysFromPubDate);
            var dtCommit = (deadline == DeadlineEnum.TwoWeeks ?
                DateTime.Parse(sPubDate).AddDays(_generalConfig.Value.Commit2WDaysFromPubDate) :
                DateTime.Parse(sPubDate).AddDays(_generalConfig.Value.Commit3WDaysFromPubDate));

            return (formsDate: dtForms, cisDate: dtCis, commitDate: dtCommit);

        }
    }
}
