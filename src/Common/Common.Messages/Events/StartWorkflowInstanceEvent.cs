using System;
using Common.Messages.Enums;

namespace Common.Messages.Events
{
    public class StartWorkflowInstanceEvent : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public WorkflowType WorkflowType { get; set; }
        public int ParentProcessId { get; set; }
        public int AssignedTaskId { get; set; }
    }
}
    