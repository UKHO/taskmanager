using Microsoft.AspNetCore.Mvc;
using NCNEWorkflowDatabase.EF;

namespace NCNEPortal.ViewComponents
{
    public class WorkflowStageViewComponent : ViewComponent
    {
        private readonly NcneWorkflowDbContext _dbContext;

        public WorkflowStageViewComponent(NcneWorkflowDbContext dbContext)
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
