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

        public IViewComponentResult Invoke(int[] parameters)
        {

            var taskStage = _dbContext.TaskStage.Find(parameters[0], parameters[1]);

            taskStage.TaskStageType = _dbContext.TaskStageType.Find(taskStage.TaskStageTypeId);

            return View(taskStage);
        }
    }
}
