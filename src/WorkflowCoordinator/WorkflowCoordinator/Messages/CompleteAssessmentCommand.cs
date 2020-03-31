using System;
using Common.Messages;

namespace WorkflowCoordinator.Messages
{
    public class CompleteAssessmentCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int ProcessId { get; set; }  
    }
}
