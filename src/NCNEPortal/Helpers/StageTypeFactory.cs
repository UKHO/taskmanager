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

        public List<TaskStageType> GetTaskStages(string chartType, string workflowType)
        {
            List<TaskStageType> taskStageTypes;

            if (workflowType == NcneWorkflowType.Withdrawal.ToString())
            {
                taskStageTypes = _dbContext.TaskStageType.Where(t =>
                    (t.TaskStageTypeId == (int)NcneTaskStageType.CIS ||
                    t.TaskStageTypeId == (int)NcneTaskStageType.V1 ||
                                 t.TaskStageTypeId == (int)NcneTaskStageType.V1_Rework ||
                    t.TaskStageTypeId >= (int)NcneTaskStageType.Withdrawal_action)).ToList();
            }
            else
            {


                if (chartType == NcneChartType.Adoption.ToString())
                    taskStageTypes = _dbContext.TaskStageType
                        .Where(t => t.TaskStageTypeId < (int)NcneTaskStageType.Withdrawal_action).ToList();
                else
                {
                    taskStageTypes = _dbContext.TaskStageType
                        .Where(t => t.TaskStageTypeId > (int)NcneTaskStageType.With_Geodesy
                                             && t.TaskStageTypeId < (int)NcneTaskStageType.Withdrawal_action).ToList();
                }
            }

            return taskStageTypes;
        }
    }
}
