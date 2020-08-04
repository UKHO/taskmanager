using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using System.Collections.Generic;
using System.Linq;

namespace DbUpdatePortal.Helpers
{
    public class StageTypeFactory : IStageTypeFactory
    {
        private readonly DbUpdateWorkflowDbContext _dbContext;

        public StageTypeFactory(DbUpdateWorkflowDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<TaskStageType> GetTaskStages(string productAction)
        {
            var taskStageTypes = _dbContext.TaskStageType.Select(t => t).ToList();

            //var tsTypes = _dbContext.TaskStageType.Select(t => t).ToList();

            ////Add the first three stages for all
            //taskStageTypes = tsTypes.Where(t => t.TaskStageTypeId <= (int)DbUpdateTaskStageType.Verification_Rework)
            //    .ToList();

            //Enum.TryParse(productAction, out DbUpdateProductAction action);

            //switch (action)
            //{
            //    case DbUpdateProductAction.None:
            //        break;
            //    case DbUpdateProductAction.SNC:
            //        taskStageTypes.Add(tsTypes.Single(t => t.TaskStageTypeId == (int)DbUpdateTaskStageType.SNC));
            //        break;
            //    case DbUpdateProductAction.ENC:
            //        taskStageTypes.Add(tsTypes.Single(t => t.TaskStageTypeId == (int)DbUpdateTaskStageType.ENC));
            //        break;
            //    default: // option "SNC & ENC" is selected
            //        taskStageTypes.AddRange(tsTypes.Where(t =>
            //            t.TaskStageTypeId > (int)DbUpdateTaskStageType.Verification_Rework));
            //        break;
            //}


            return taskStageTypes;

        }
    }
}
