using System;
using Common.Messages;

namespace WorkflowCoordinator.Messages
{
    public class StartDbAssessmentCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SourceDocumentId { get; set; }
    }
}
