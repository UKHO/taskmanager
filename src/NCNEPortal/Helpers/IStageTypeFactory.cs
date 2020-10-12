using NCNEWorkflowDatabase.EF.Models;
using System.Collections.Generic;

namespace NCNEPortal.Helpers
{
    public interface IStageTypeFactory
    {
        List<TaskStageType> GetTaskStages(string chartType, string workflowType);
    }
}
