using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkflowDatabase.EF;

namespace Portal.BusinessLogic
{
    public class WorkflowBusinessLogicService : IWorkflowBusinessLogicService
    {
        private readonly WorkflowDbContext _dbContext;
        private readonly ILogger<WorkflowBusinessLogicService> _logger;

        public WorkflowBusinessLogicService(WorkflowDbContext dbContext,
            ILogger<WorkflowBusinessLogicService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> WorkflowIsReadOnlyAsync(int processId)
        {
            var workflowInstance = await _dbContext.WorkflowInstance
                .AsNoTracking()
                .FirstOrDefaultAsync(wi => wi.ProcessId == processId);

            if (workflowInstance == null)
            {
                _logger.LogError("ProcessId {ProcessId} does not appear in the WorkflowInstance table", processId);
                throw new ArgumentException($"{nameof(processId)} {processId} does not appear in the WorkflowInstance table");
            }

            return workflowInstance.Status == WorkflowStatus.Terminated.ToString() || 
                   workflowInstance.Status == WorkflowStatus.Completed.ToString();
        }
    }
}
