using System;

namespace Common.Messages.Commands
{
    public class CompleteAssessmentCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int ProcessId { get; set; }  
    }
}
