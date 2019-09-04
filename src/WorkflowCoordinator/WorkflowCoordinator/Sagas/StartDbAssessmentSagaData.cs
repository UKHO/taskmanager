using System;
using Common.Messages;
using NServiceBus;

namespace WorkflowCoordinator.Sagas
{
    public class StartDbAssessmentSagaData : ContainSagaData, ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public int SourceDocumentId { get; set; }
        public int ProcessId { get; set; }
        public bool IsStarted { get; set; }
    }
}
