using System;
using Common.Messages;

namespace WorkflowCoordinator.Messages
{
    public class RetrieveAssessmentDataCommand : ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SourceDocumentId { get; set; }
    }
}
