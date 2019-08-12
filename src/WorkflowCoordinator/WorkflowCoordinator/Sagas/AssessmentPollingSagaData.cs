using System;
using Common.Messages;
using NServiceBus;

namespace WorkflowCoordinator.Sagas
{
    public class AssessmentPollingSagaData : ContainSagaData, ICorrelate
    {
        public Guid CorrelationId { get; set; }
        public bool IsTaskAlreadyScheduled { get; set; }
    }
}
