using NCNEPortal.Enums;
using NCNEWorkflowDatabase.EF;
using NCNEWorkflowDatabase.EF.Models;
using System.Collections.Generic;
using System.Linq;

namespace NCNEPortal.Helpers
{
    public class StageTypeFactory : IStageTypeFactory
    {
        private readonly NcneWorkflowDbContext _dbContext;

        public StageTypeFactory(NcneWorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<TaskStageType> GeTaskStages(string chartType)
        {
            List<TaskStageType> taskStageTypes;

            if (chartType == NcneChartType.Adoption.ToString())
                taskStageTypes = _dbContext.TaskStageType.Select(t => t).ToList();
            else
            {
                taskStageTypes = _dbContext.TaskStageType
                    .Where(t => t.TaskStageTypeId > (int)NcneTaskStageType.With_Geodesy).ToList();
            }

            return taskStageTypes;
        }
    }
}
