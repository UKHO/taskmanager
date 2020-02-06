using NCNEPortal.Enums;
using System;

namespace NCNEPortal.Calculators
{
    public interface IMilestoneCalculator
    {
        (DateTime formsDate, DateTime cisDate, DateTime commitDate) CalculateMilestones(DeadlineEnum deadline,
            DateTime publicationDate);
    }
}
