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

            return taskStageTypes;

        }
    }
}
