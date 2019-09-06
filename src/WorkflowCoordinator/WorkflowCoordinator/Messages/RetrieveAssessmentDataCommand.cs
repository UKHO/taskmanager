using System;
using Common.Messages;

namespace WorkflowCoordinator.Messages
{
    public class RetrieveAssessmentDataCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SourceDocumentId { get; set; }
        public int ProcessId { get; set; }
        public int WorkflowInstanceId { get; set; }
    }
}
