using System;

namespace Common.Messages
{
    public class AnnounceOpenedAssessmentMessage : ICorrelation
    {
        public Guid CorrelationId { get; set; }
    }
}
