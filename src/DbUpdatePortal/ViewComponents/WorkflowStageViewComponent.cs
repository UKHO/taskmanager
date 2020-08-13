using DbUpdatePortal.Enums;
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

            taskStage.TaskStageType.DisplayName = (DbUpdateTaskStageType)taskStage.TaskStageTypeId switch
            {
                DbUpdateTaskStageType.ENC => "Notify DCPT",
                DbUpdateTaskStageType.SNC => "Notify CPT",
                _ => taskStage.TaskStageType.Name
            };

            return View(taskStage);
        }
    }
}
