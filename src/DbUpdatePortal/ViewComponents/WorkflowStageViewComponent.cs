using DbUpdateWorkflowDatabase.EF;
using Microsoft.AspNetCore.Mvc;

namespace DbUpdatePortal.ViewComponents
{
    public class WorkflowStageViewComponent : ViewComponent
    {
        private readonly DbUpdateWorkflowDbContext _dbContext;

        public WorkflowStageViewComponent(DbUpdateWorkflowDbContext dbContext)
        {

            _dbContext = dbContext;

        }

        public IViewComponentResult Invoke(int processId, int taskStageId, bool isReadOnly)
        {

            var taskStage = _dbContext.TaskStage.Find(processId, taskStageId);

            taskStage.IsReadOnly = isReadOnly;

            return View(taskStage);
        }
    }
}
