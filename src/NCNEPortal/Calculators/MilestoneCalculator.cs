using System;
using NCNEPortal.Enums;

namespace NCNEPortal.Calculators
{
    public class MilestoneCalculator : IMileStoneCalculator
    {
        public (DateTime formsDate, DateTime cisDate, DateTime commitDate) CalculateMilestones(DeadlineEnum deadline,
            DateTime publicationDate)
        {
            var dtForms = publicationDate.AddDays(-36);
            var dtCis = publicationDate.AddDays(-6);
            var dtCommit = (deadline == DeadlineEnum.TwoWeeks ? publicationDate.AddDays(-15) : publicationDate.AddDays(-21));

            return (formsDate: dtForms, cisDate: dtCis, commitDate: dtCommit);

        }
    }
}
