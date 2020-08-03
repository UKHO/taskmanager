using DbUpdatePortal.Enums;
using DbUpdateWorkflowDatabase.EF;
using DbUpdateWorkflowDatabase.EF.Models;
using System;
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
            List<TaskStageType> taskStageTypes;

            var tsTypes = _dbContext.TaskStageType.Select(t => t).ToList();

            //Add the first three stages for all
            taskStageTypes = tsTypes.Where(t => t.TaskStageTypeId <= (int)DbUpdateTaskStageType.Verification_Rework)
                .ToList();

            Enum.TryParse(productAction, out DbUpdateProductAction action);

            switch (action)
            {
                case DbUpdateProductAction.None:
                    break;
                case DbUpdateProductAction.SNC:
                    taskStageTypes.Add(tsTypes.Single(t => t.TaskStageTypeId == (int)DbUpdateTaskStageType.SNC));
                    break;
                case DbUpdateProductAction.ENC:
                    taskStageTypes.Add(tsTypes.Single(t => t.TaskStageTypeId == (int)DbUpdateTaskStageType.ENC));
                    break;
                case DbUpdateProductAction.BOTH:
                    taskStageTypes.AddRange(tsTypes.Where(t =>
                        t.TaskStageTypeId > (int)DbUpdateTaskStageType.Verification_Rework));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            return taskStageTypes;

        }
    }
}
