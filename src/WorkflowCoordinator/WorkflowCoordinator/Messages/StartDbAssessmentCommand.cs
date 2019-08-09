using System;
using Common.Messages;

namespace WorkflowCoordinator.Messages
{
    public class StartDbAssessmentCommand : ICorrelation
    {
        public Guid CorrelationId { get; set; }
    }
}
