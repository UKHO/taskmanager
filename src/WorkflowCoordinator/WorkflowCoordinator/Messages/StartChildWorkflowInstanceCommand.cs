using System;
using Common.Messages;
using Common.Messages.Enums;

namespace WorkflowCoordinator.Messages
{
    public class StartChildWorkflowInstanceCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public WorkflowType WorkflowType { get; set; }
        public int ParentProcessId { get; set; }
        public int AssignedTaskId { get; set; }
    }
}
    