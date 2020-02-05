using System;
using NCNEPortal.Enums;

namespace NCNEPortal.Calculators
{
    public interface IMileStoneCalculator
    {
        (DateTime formsDate, DateTime cisDate, DateTime commitDate) CalculateMilestones(DeadlineEnum deadline,
            DateTime publicationDate);
    }
}
