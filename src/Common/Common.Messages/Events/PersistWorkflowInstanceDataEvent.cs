using System;

namespace Common.Messages.Events
{
    public class PersistWorkflowInstanceDataEvent : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int ProcessId { get; set; }
        public string FromActivityName { get; set; }
        public string ToActivityName { get; set; }
    }   
}
