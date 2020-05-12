using System;
using System.Threading.Tasks;

namespace Portal.BusinessLogic
{
    public interface IWorkflowBusinessLogicService
    {
        /// <summary>
        /// Used to determine if the workflow for the given processId is read only
        /// </summary>
        /// <param name="processId">The processId for the workflow</param>
        /// <returns>True if read only, false if not read only</returns>
        /// <exception cref="ArgumentException">If workflow for given processId is not found</exception>
        Task<bool> WorkflowIsReadOnlyAsync(int processId);
    }
}