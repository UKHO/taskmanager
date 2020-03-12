using NCNEWorkflowDatabase.EF.Models;
using System.Collections.Generic;

namespace NCNEPortal.Helpers
{
    public interface IStageTypeFactory
    {
        List<TaskStageType> GeTaskStages(string chartType);
    }
}
