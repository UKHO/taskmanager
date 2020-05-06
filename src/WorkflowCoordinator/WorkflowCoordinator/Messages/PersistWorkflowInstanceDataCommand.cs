using System;
using Common.Messages;
using WorkflowDatabase.EF;

namespace WorkflowCoordinator.Messages
{
    public class PersistWorkflowInstanceDataCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int ProcessId { get; set; }
        public WorkflowStage FromActivity { get; set; }
        public WorkflowStage ToActivity { get; set; }
    }
}
