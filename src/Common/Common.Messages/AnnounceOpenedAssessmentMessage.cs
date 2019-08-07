using System;
using NServiceBus;

namespace Common.Messages
{
    public class AnnounceOpenedAssessmentMessage : IMessage, ICorrelation
    {
        public Guid CorrelationId { get; set; }
    }
}
