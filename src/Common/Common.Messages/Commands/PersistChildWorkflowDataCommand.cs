using System;

namespace Common.Messages.Commands
{
    public class PersistChildWorkflowDataCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int ChildProcessId { get;set; }
        public int ParentProcessId { get; set; }
        public int AssignedTaskId { get; set; }
    }
}
