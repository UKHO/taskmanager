using System;
using Common.Messages;

namespace WorkflowCoordinator.Messages
{
    public class StartAssessmentPollingCommand : ICorrelate
    {
        /// <summary>
        /// This guid should always be the same
        /// </summary>
        /// <param name="staticGuid"></param>
        public StartAssessmentPollingCommand(Guid staticGuid)
        {
            CorrelationId = staticGuid;
        }

        public Guid CorrelationId { get; set; }
    }
}
