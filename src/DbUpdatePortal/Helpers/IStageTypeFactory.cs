using DbUpdateWorkflowDatabase.EF.Models;
using System.Collections.Generic;

namespace DbUpdatePortal.Helpers
{
    public interface IStageTypeFactory
    {
        List<TaskStageType> GetTaskStages(string productAction);
    }
}
