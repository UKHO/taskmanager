using System;

namespace NCNEPortal.Calculators
{
    public interface IMileStoneCalculator
    {
        (DateTime formsDate, DateTime cisDate, DateTime commitDate) CalculateMilestones(String deadline,
            DateTime publicationDate);
    }
}
